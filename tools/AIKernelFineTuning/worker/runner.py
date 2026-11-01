"""One fixed training entry point in a supervised process group."""

import argparse
import hashlib
import importlib.metadata
import json
import math
import os
import random
import signal
import sys
import threading
import time
import traceback
from pathlib import Path

from . import VERSION
from .config import OFFLINE_ENV, WorkerError
from .datasets import encode_row, split_rows, validate_dataset
from .models import validate_model_request
from .storage import atomic_write, utc_now

STOP = threading.Event()


def parent_watch(fd):
    try:
        while os.read(fd, 1):
            pass
    finally:
        os.close(fd)
        STOP.set()
        # A dead supervisor cannot enforce a grace timeout.
        timer = threading.Timer(30, lambda: os.killpg(os.getpgrp(), signal.SIGKILL))
        timer.daemon = True
        timer.start()


class Events:
    def __init__(self, output):
        self.output = output
        self.metrics = (output / "metrics.jsonl").open("a", buffering=1)
        self.count = 0

    def emit(self, kind, message="", **metrics):
        for name, value in metrics.items():
            if value is not None and not math.isfinite(value):
                raise RuntimeError(f"Non-finite training metric: {name}")
        event = dict(timestampUtc=utc_now(), kind=kind, message=message, metrics=metrics)
        print(json.dumps(event, allow_nan=False), flush=True)
        if kind == "metrics":
            self.metrics.write(json.dumps(event, allow_nan=False) + "\n")
            self.count += 1
            if self.count % 20 == 0:
                self.metrics.flush()
                os.fsync(self.metrics.fileno())

    def close(self):
        self.metrics.flush()
        os.fsync(self.metrics.fileno())
        self.metrics.close()


def batch_groups(rows, request, include_indices=False):
    rng = random.Random(request["seed"])
    epoch = 0
    while request["maxSteps"] or epoch < request["epochs"]:
        order = list(range(len(rows)))
        rng.shuffle(order)
        batches = [
            [rows[index] for index in order[start:start + request["batchSize"]]]
            for start in range(0, len(order), request["batchSize"])
        ]
        accumulation = request["gradientAccumulationSteps"]
        for start in range(0, len(batches), accumulation):
            group = batches[start:start + accumulation]
            if include_indices:
                indices = [
                    index for batch_start in range(start, min(start + accumulation, len(batches)))
                    for index in order[batch_start * request["batchSize"]: (batch_start + 1) * request["batchSize"]]
                ]
                yield group, epoch + min(1, (start + len(group)) / len(batches)), indices
            else:
                yield group, epoch + min(1, (start + len(group)) / len(batches))
        epoch += 1


def schedule_rate(request, step, total):
    warmup = math.ceil(total * request["warmupRatio"])
    if warmup and step <= warmup:
        return request["learningRate"] * step / warmup
    return request["learningRate"] * max(0, (total - step + 1) / max(1, total - warmup))


def dataset_digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def sample_index_text(indices):
    if len(indices) <= 32:
        return ",".join(str(index) for index in indices)
    return ",".join(str(index) for index in indices[:16]) + ",...," + ",".join(str(index) for index in indices[-16:])


def train(spec):
    request = spec["request"]
    output = Path(spec["outputPath"])
    output.mkdir(parents=True, exist_ok=True)
    events = Events(output)
    started = time.monotonic()
    backend = None
    step = 0
    manifest = {
        "workerVersion": VERSION, "jobId": spec["jobId"], "request": request,
        "createdUtc": utc_now(), "baseModelId": request["modelId"],
        "datasetSha256": dataset_digest(Path(spec["datasetPath"])),
        "evaluationDatasetSha256": dataset_digest(Path(spec["evalDatasetPath"])) if spec["evalDatasetPath"] else None,
        "evaluationStrategy": "explicitHeldOut" if spec["evalDatasetPath"] else "seededDistinctRowSplit20PercentMinimum1",
        "lossObjective": "causalLanguageModelAllNonPaddingTokens",
        "sequencePolicy": "truncateRightAtMaxSequenceLength",
        "deployment": "Adapter export only. Not a deployed or registered NCF inference model. Requires the same local base model and compatible backend.",
        "resumeSupported": True, "checkpointPolicy": "Last two completed checkpoints, including adapter, optimizer state and RNG.",
        "status": "initializing",
    }
    atomic_write(output / "manifest.json", json.dumps(manifest, indent=2).encode())
    try:
        model_path, _ = validate_model_request(Path(spec["modelsRoot"]), request)
        manifest["baseModelSha256"] = {
            path.name: dataset_digest(path) for path in sorted(model_path.glob("*.safetensors"))
        }
        dependencies = ("transformers", "mlx", "mlx-lm") if request["backend"] == "mlx" else (
            "torch", "transformers", "peft", "accelerate")
        manifest["dependencyVersions"] = {name: importlib.metadata.version(name) for name in dependencies}
        from transformers import AutoTokenizer

        tokenizer = AutoTokenizer.from_pretrained(model_path, local_files_only=True, trust_remote_code=False)
        if tokenizer.pad_token_id is None:
            if tokenizer.eos_token_id is None:
                raise WorkerError("Tokenizer requires an EOS or padding token.")
            tokenizer.pad_token = tokenizer.eos_token
        rows = validate_dataset(Path(spec["datasetPath"]).read_text())[0]
        eval_rows = validate_dataset(Path(spec["evalDatasetPath"]).read_text())[0] if spec["evalDatasetPath"] else None
        training, evaluation = split_rows(rows, eval_rows, request["seed"])
        train_tokens = [encode_row(tokenizer, row, request["maxSequenceLength"]) for row in training]
        eval_tokens = [encode_row(tokenizer, row, request["maxSequenceLength"]) for row in evaluation]
        manifest.update(trainingRows=len(training), evaluationRows=len(evaluation))
        events.emit(
            "log",
            f"Prepared dataset: trainingRows={len(training)}, evaluationRows={len(evaluation)}, "
            f"maxSequenceLength={request['maxSequenceLength']}. Sample indices refer to the training split.",
            trainingRows=len(training), evaluationRows=len(evaluation),
            maxSequenceLength=request["maxSequenceLength"],
        )
        tokenizer.save_pretrained(output / "tokenizer")
        if any(len(tokens) == request["maxSequenceLength"] for tokens in train_tokens + eval_tokens):
            events.emit("warning", "Rows at the sequence limit may be right-truncated; increase maxSequenceLength if needed.")
        if STOP.is_set():
            manifest.update(completedSteps=0, status="cancelled")
            events.emit("warning", "Stopped during initialization; no optimizer step/checkpoint is available.")
            return 143
        if request["backend"] == "mlx":
            from .training_mlx import MlxTraining
            backend = MlxTraining(model_path, request, tokenizer.pad_token_id)
        else:
            from .training_torch import TorchTraining
            backend = TorchTraining(model_path, request, tokenizer.pad_token_id)
        updates_per_epoch = math.ceil(math.ceil(len(train_tokens) / request["batchSize"]) / request["gradientAccumulationSteps"])
        total = request["maxSteps"] or request["epochs"] * updates_per_epoch
        manifest["totalSteps"] = total
        resume_step = int(spec.get("resumeStep", 0))
        if resume_step < 0 or resume_step > total:
            raise WorkerError("Checkpoint step is outside the current training plan.")
        if resume_step:
            backend.load_checkpoint(Path(spec["resumeCheckpoint"]))
            manifest["resumedFromStep"] = resume_step
        events.emit("log", "Offline local model loaded. Training updates adapter parameters only.")
        tokens_seen, loss_sum, logging_count = 0, 0.0, 0
        step_time_sum, timed_steps = 0.0, 0
        last_eval = None
        for batches, epoch, sample_indices in batch_groups(train_tokens, request, include_indices=True):
            if STOP.is_set() or step >= total:
                break
            if step < resume_step:
                step += 1
                continue
            step += 1
            rate = schedule_rate(request, step, total)
            sample_lengths = [len(row) for batch in batches for row in batch]
            sample_token_count = sum(max(0, length - 1) for length in sample_lengths)
            sample_metrics = dict(
                step=step,
                epoch=epoch,
                sampleCount=len(sample_indices),
                sampleIndexMin=min(sample_indices),
                sampleIndexMax=max(sample_indices),
                microBatchCount=len(batches),
                tokenCount=sample_token_count,
                sequenceLengthMin=min(sample_lengths),
                sequenceLengthMax=max(sample_lengths),
                truncatedSampleCount=sum(length == request["maxSequenceLength"] for length in sample_lengths),
            )
            events.emit(
                "log",
                f"Training samples for step {step}: [{sample_index_text(sample_indices)}]; "
                f"sampleCount={len(sample_indices)}, tokenCount={sample_token_count}, "
                f"sequenceLength={min(sample_lengths)}-{max(sample_lengths)}.",
                **sample_metrics,
            )
            step_started = time.monotonic()
            loss, norm, count = backend.update(batches, rate)
            step_duration = time.monotonic() - step_started
            step_time_sum += step_duration
            timed_steps += 1
            if not math.isfinite(loss) or not math.isfinite(norm):
                raise RuntimeError("Training diverged: non-finite loss or gradient norm.")
            loss_sum += loss
            logging_count += 1
            tokens_seen += count
            elapsed = time.monotonic() - started
            if step % request["loggingSteps"] == 0 or step == total:
                average_step = step_time_sum / max(1, timed_steps)
                metrics = dict(trainLoss=loss_sum / logging_count, step=step, totalSteps=total,
                               epoch=epoch, learningRate=rate, gradNorm=norm,
                               tokensPerSecond=tokens_seen / max(elapsed, 1e-9), elapsedSeconds=elapsed,
                               stepDurationSeconds=step_duration,
                               averageStepDurationSeconds=average_step,
                               estimatedRemainingSeconds=average_step * max(total - step, 0))
                metrics.update(sample_metrics)
                metrics.update(backend.memory_metrics())
                events.emit("metrics", "Training optimizer update", **metrics)
                loss_sum, logging_count = 0.0, 0
            if not STOP.is_set() and (step % request["evalSteps"] == 0 or step == total):
                last_eval = backend.evaluate(eval_tokens, request["batchSize"], STOP)
                if last_eval is not None:
                    events.emit("metrics", "Held-out evaluation", evalLoss=last_eval, step=step, totalSteps=total,
                                elapsedSeconds=time.monotonic() - started)
            if step % request["saveSteps"] == 0 or step == total or STOP.is_set():
                backend.checkpoint(output / "checkpoints", step)
                events.emit("log", f"Completed checkpoint at optimizer step {step}.")
        if step:
            backend.checkpoint(output / "checkpoints", step)
            if not STOP.is_set() or not (output / "adapter").is_dir():
                backend.export(output / "adapter")
            manifest.update(completedSteps=step, evalLoss=last_eval, status="cancelled" if STOP.is_set() else "succeeded")
            if STOP.is_set():
                events.emit("warning", "Cancellation checkpoint is partial training, not a successful deployment.")
        else:
            manifest.update(completedSteps=0, status="cancelled")
            events.emit("warning", "Stopped before the first update; no trained adapter is available.")
        return 143 if STOP.is_set() else 0
    except Exception as exc:
        # The fixed child boundary must report unexpected framework failures to its supervisor.
        manifest.update(status="failed", error=str(exc), completedSteps=step)
        events.emit("error", f"{type(exc).__name__}: {exc}")
        traceback.print_exc()
        return 1
    finally:
        manifest["finishedUtc"] = utc_now()
        manifest["elapsedSeconds"] = time.monotonic() - started
        atomic_write(output / "manifest.json", json.dumps(manifest, indent=2, allow_nan=False).encode())
        events.close()


def main():
    os.environ.update(OFFLINE_ENV)
    parser = argparse.ArgumentParser()
    parser.add_argument("--spec", required=True)
    parser.add_argument("--parent-fd", required=True, type=int)
    args = parser.parse_args()
    signal.signal(signal.SIGTERM, lambda *_: STOP.set())
    signal.signal(signal.SIGINT, lambda *_: STOP.set())
    threading.Thread(target=parent_watch, args=(args.parent_fd,), daemon=True).start()
    with open(args.spec) as stream:
        spec = json.load(stream)
    sys.exit(train(spec))


if __name__ == "__main__":
    main()
