"""Fixed offline capability and model preflight subprocess; no user commands."""

import argparse
import importlib.util
import json
import platform
import sys

from .config import OFFLINE_ENV, WorkerError


def capabilities():
    cpu = {"backend": "cpu", "methods": ["lora"], "available": False, "reason": None}
    cuda = {"backend": "cuda", "methods": ["lora"], "available": False, "reason": None}
    mlx = {"backend": "mlx", "methods": ["lora", "qlora"], "available": False, "reason": None}
    try:
        import torch
        import peft  # noqa: F401
        import transformers  # noqa: F401
        import accelerate  # noqa: F401

        cpu["available"] = True
        cuda["available"] = torch.cuda.is_available()
        if not cuda["available"]:
            cuda["reason"] = "No usable CUDA GPU in this process."
        elif importlib.util.find_spec("bitsandbytes") is not None:
            import bitsandbytes  # noqa: F401
            cuda["methods"].append("qlora")
        else:
            cuda["reason"] = "LoRA available; install CUDA requirements for bitsandbytes NF4 QLoRA."
    except (ImportError, OSError, RuntimeError) as exc:
        cpu["reason"] = f"PyTorch/PEFT dependency unavailable: {exc}"
        cuda["reason"] = cpu["reason"]
    if platform.system() != "Darwin" or platform.machine() != "arm64":
        mlx["reason"] = "MLX requires native macOS on Apple Silicon; Linux Docker cannot use Metal."
    else:
        try:
            import mlx.core as mx
            import mlx_lm  # noqa: F401

            mlx["available"] = mx.metal.is_available()
            if not mlx["available"]:
                mlx["reason"] = "Metal device is unavailable."
        except (ImportError, OSError, RuntimeError) as exc:
            mlx["reason"] = f"MLX dependency unavailable: {exc}"
    return [cpu, cuda, mlx]


def preflight(spec):
    from pathlib import Path

    from .datasets import encode_row, split_rows, validate_dataset
    from .models import validate_model_request

    request = spec["request"]
    model_path, _ = validate_model_request(Path(spec["modelsRoot"]), request)
    rows = validate_dataset(Path(spec["datasetPath"]).read_text())[0]
    eval_rows = validate_dataset(Path(spec["evalDatasetPath"]).read_text())[0] if spec["evalDatasetPath"] else None
    train, evaluation = split_rows(rows, eval_rows, request["seed"])
    from transformers import AutoTokenizer

    tokenizer = AutoTokenizer.from_pretrained(model_path, local_files_only=True, trust_remote_code=False)
    encoded = [encode_row(tokenizer, row, request["maxSequenceLength"]) for row in train + evaluation]
    if request["backend"] != "mlx":
        from accelerate import init_empty_weights
        from peft import LoraConfig, get_peft_model
        from transformers import AutoConfig, AutoModelForCausalLM

        config = AutoConfig.from_pretrained(model_path, local_files_only=True, trust_remote_code=False)
        with init_empty_weights():
            model = AutoModelForCausalLM.from_config(config, trust_remote_code=False)
            get_peft_model(model, lora_config(request))
        vocabulary = config.vocab_size
        if any(max(tokens) >= vocabulary or min(tokens) < 0 for tokens in encoded):
            raise WorkerError("Tokenizer token IDs exceed the local model vocabulary.")
    else:
        import mlx.nn as nn
        from mlx_lm.utils import load_model
        from mlx_lm.tuner.utils import linear_to_lora_layers

        model, _ = load_model(model_path, lazy=True, strict=True)
        if request["method"] == "qlora" and not any(isinstance(module, nn.QuantizedLinear) for _, module in model.named_modules()):
            nn.quantize(model, group_size=64, bits=4)
        model.freeze()
        linear_to_lora_layers(model, len(model.layers), mlx_lora_config(model, request))
        from mlx.utils import tree_flatten
        if not tree_flatten(model.trainable_parameters()):
            raise WorkerError("No MLX trainable target modules matched.")
        if any(max(tokens) >= model.args.vocab_size or min(tokens) < 0 for tokens in encoded):
            raise WorkerError("Tokenizer token IDs exceed the local model vocabulary.")
    return {"trainingRows": len(train), "evaluationRows": len(evaluation)}


def lora_config(request):
    from peft import LoraConfig

    targets = request["targetModules"]
    return LoraConfig(
        task_type="CAUSAL_LM", r=request["loraRank"], lora_alpha=request["loraAlpha"],
        lora_dropout=request["loraDropout"], bias="none",
        target_modules=targets if targets == "all-linear" else targets.split(","),
    )


def mlx_lora_config(model, request):
    import mlx.nn as nn

    modules = {
        name for layer in model.layers for name, module in layer.named_modules()
        if isinstance(module, (nn.Linear, nn.QuantizedLinear))
    }
    targets = request["targetModules"]
    if targets == "all-linear":
        keys = sorted(modules)
    else:
        requested = targets.split(",")
        for target in requested:
            if not any(name == target or name.endswith("." + target) for name in modules):
                raise WorkerError(f"MLX target module not found: {target}")
        keys = sorted(name for name in modules if any(name == target or name.endswith("." + target) for target in requested))
    if not keys:
        raise WorkerError("No MLX linear target modules found.")
    return {"rank": request["loraRank"], "scale": request["loraAlpha"] / request["loraRank"],
            "dropout": request["loraDropout"], "keys": keys}


def main():
    import os
    os.environ.update(OFFLINE_ENV)
    parser = argparse.ArgumentParser()
    parser.add_argument("operation", choices=["capabilities", "preflight"])
    args = parser.parse_args()
    try:
        result = capabilities() if args.operation == "capabilities" else preflight(json.load(sys.stdin))
        print(json.dumps({"ok": True, "result": result}, allow_nan=False))
    except (WorkerError, ValueError, TypeError, KeyError, OSError, ImportError, RuntimeError) as exc:
        print(json.dumps({"ok": False, "error": str(exc)}))
        sys.exit(1)


if __name__ == "__main__":
    main()
