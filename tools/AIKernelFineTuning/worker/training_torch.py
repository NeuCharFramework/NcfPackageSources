import json
import os
import random
import shutil

from .probe import lora_config
from .storage import atomic_write


class TorchTraining:
    def __init__(self, model_path, request, pad_id):
        import numpy as np
        import torch
        from peft import get_peft_model, prepare_model_for_kbit_training
        from transformers import AutoModelForCausalLM, BitsAndBytesConfig

        self.torch = torch
        self.request = request
        self.pad_id = pad_id
        self.device = torch.device("cuda" if request["backend"] == "cuda" else "cpu")
        torch.set_num_threads(int(os.environ.get("NCF_TORCH_THREADS", "2")))
        random.seed(request["seed"])
        np.random.seed(request["seed"])
        torch.manual_seed(request["seed"])
        if self.device.type == "cuda":
            torch.cuda.manual_seed_all(request["seed"])
        self.dtype = torch.float32 if self.device.type == "cpu" else (
            torch.bfloat16 if torch.cuda.is_bf16_supported() else torch.float16
        )
        options = dict(local_files_only=True, trust_remote_code=False, use_safetensors=True,
                       torch_dtype=self.dtype, attn_implementation="eager")
        if request["method"] == "qlora":
            options.update(
                device_map={"": 0},
                quantization_config=BitsAndBytesConfig(
                    load_in_4bit=True, bnb_4bit_quant_type="nf4", bnb_4bit_use_double_quant=True,
                    bnb_4bit_compute_dtype=self.dtype,
                ),
            )
        model = AutoModelForCausalLM.from_pretrained(model_path, **options)
        if request["method"] == "qlora":
            model = prepare_model_for_kbit_training(model, use_gradient_checkpointing=True)
        else:
            model.to(self.device)
        model.config.use_cache = False
        self.model = get_peft_model(model, lora_config(request))
        self.parameters = [parameter for parameter in self.model.parameters() if parameter.requires_grad]
        if not self.parameters:
            raise RuntimeError("No trainable adapter parameters.")
        self.optimizer = torch.optim.AdamW(self.parameters, lr=request["learningRate"], weight_decay=request["weightDecay"])
        self.scaler = torch.amp.GradScaler("cuda", enabled=self.device.type == "cuda" and self.dtype == torch.float16)

    def batch(self, rows):
        torch = self.torch
        width = max(map(len, rows))
        ids = torch.full((len(rows), width), self.pad_id, dtype=torch.long, device=self.device)
        mask = torch.zeros_like(ids)
        labels = torch.full_like(ids, -100)
        for index, tokens in enumerate(rows):
            ids[index, :len(tokens)] = torch.tensor(tokens, dtype=torch.long, device=self.device)
            mask[index, :len(tokens)] = 1
            labels[index, :len(tokens)] = ids[index, :len(tokens)]
        return dict(input_ids=ids, attention_mask=mask, labels=labels)

    def autocast(self):
        return self.torch.autocast(device_type=self.device.type, dtype=self.dtype, enabled=self.device.type == "cuda")

    def update(self, batches, rate):
        self.model.train()
        self.optimizer.zero_grad(set_to_none=True)
        for group in self.optimizer.param_groups:
            group["lr"] = rate
        loss_value, count = 0.0, 0
        for rows in batches:
            with self.autocast():
                loss = self.model(**self.batch(rows)).loss
            self.scaler.scale(loss / len(batches)).backward()
            loss_value += float(loss.detach().float().cpu()) / len(batches)
            count += sum(len(row) - 1 for row in rows)
        self.scaler.unscale_(self.optimizer)
        norm = self.torch.nn.utils.clip_grad_norm_(self.parameters, 1.0, error_if_nonfinite=True)
        self.scaler.step(self.optimizer)
        self.scaler.update()
        return loss_value, float(norm.float().cpu()), count

    def evaluate(self, rows, batch_size, stop):
        self.model.eval()
        loss_sum, total_tokens = 0.0, 0
        with self.torch.no_grad():
            for start in range(0, len(rows), batch_size):
                if stop.is_set():
                    return None
                batch_rows = rows[start:start + batch_size]
                with self.autocast():
                    loss = self.model(**self.batch(batch_rows)).loss
                count = sum(len(row) - 1 for row in batch_rows)
                loss_sum += float(loss.float().cpu()) * count
                total_tokens += count
        return loss_sum / total_tokens

    def memory_metrics(self):
        if self.device.type == "cuda":
            return dict(gpuAllocatedBytes=self.torch.cuda.memory_allocated(),
                        gpuPeakAllocatedBytes=self.torch.cuda.max_memory_allocated())
        return {}

    def export(self, path):
        path.mkdir(exist_ok=True)
        self.model.save_pretrained(path, safe_serialization=True)
        self.model.config.save_pretrained(path)

    def checkpoint(self, root, step):
        root.mkdir(exist_ok=True)
        destination = root / f"step-{step:08d}"
        if destination.exists():
            return
        temporary = root / f"step-{step:08d}.tmp"
        temporary.mkdir()
        self.export(temporary)
        self.torch.save({
            "optimizer": self.optimizer.state_dict(), "scaler": self.scaler.state_dict(),
            "torchRngState": self.torch.get_rng_state(),
            "cudaRngState": self.torch.cuda.get_rng_state_all() if self.device.type == "cuda" else [],
            "step": step,
        }, temporary / "optimizer-state.pt")
        atomic_write(temporary / "checkpoint.json", json.dumps({
            "step": step, "backend": self.request["backend"],
            "optimizerState": "Trusted worker-generated PyTorch state, not accepted as an input model.",
            "resumeSupported": False,
        }).encode())
        os.replace(temporary, destination)
        for old in sorted(path for path in root.iterdir() if path.is_dir() and not path.name.endswith(".tmp"))[:-2]:
            shutil.rmtree(old)
