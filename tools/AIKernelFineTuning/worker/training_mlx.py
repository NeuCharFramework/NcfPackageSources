import json
import os
import shutil

from .probe import mlx_lora_config
from .storage import atomic_write


class MlxTraining:
    def __init__(self, model_path, request, pad_id):
        import mlx.core as mx
        import mlx.nn as nn
        import mlx.optimizers as optim
        from mlx_lm.utils import load_model
        from mlx_lm.tuner.utils import linear_to_lora_layers

        self.mx, self.nn = mx, nn
        self.request, self.pad_id = request, pad_id
        mx.random.seed(request["seed"])
        self.model, self.config = load_model(model_path, lazy=False, strict=True)
        if request["method"] == "qlora" and not self.config.get("quantization"):
            nn.quantize(self.model, group_size=64, bits=4)
            self.config["quantization"] = {"group_size": 64, "bits": 4, "mode": "affine"}
        self.model.freeze()
        self.lora_parameters = mlx_lora_config(self.model, request)
        linear_to_lora_layers(self.model, len(self.model.layers), self.lora_parameters)
        self.optimizer = optim.AdamW(learning_rate=request["learningRate"], weight_decay=request["weightDecay"])
        self.value_and_grad = nn.value_and_grad(self.model, self.loss)
        mx.eval(self.model.parameters())

    def batch(self, rows):
        width = max(map(len, rows))
        ids = self.mx.array([row + [self.pad_id] * (width - len(row)) for row in rows])
        mask = self.mx.array([[1] * (len(row) - 1) + [0] * (width - len(row)) for row in rows])
        return ids, mask

    def loss(self, model, ids, mask):
        logits = model(ids[:, :-1])
        losses = self.nn.losses.cross_entropy(logits.astype(self.mx.float32), ids[:, 1:])
        return (losses * mask).sum() / mask.sum()

    def update(self, batches, rate):
        from mlx.utils import tree_flatten, tree_map

        self.model.train()
        gradients, loss_value, count = None, 0.0, 0
        for rows in batches:
            ids, mask = self.batch(rows)
            loss, batch_gradients = self.value_and_grad(self.model, ids, mask)
            gradients = batch_gradients if gradients is None else tree_map(lambda a, b: a + b, gradients, batch_gradients)
            self.mx.eval(loss, gradients)
            loss_value += loss.item() / len(batches)
            count += sum(len(row) - 1 for row in rows)
        gradients = tree_map(lambda value: value / len(batches), gradients)
        norm = self.mx.sqrt(sum((value.astype(self.mx.float32) ** 2).sum() for _, value in tree_flatten(gradients)))
        self.mx.eval(norm)
        scale = self.mx.minimum(1.0, 1.0 / (norm + 1e-6))
        gradients = tree_map(lambda value: value * scale, gradients)
        self.optimizer.learning_rate = rate
        self.optimizer.update(self.model, gradients)
        self.mx.eval(self.model.parameters(), self.optimizer.state)
        return loss_value, norm.item(), count

    def evaluate(self, rows, batch_size, stop):
        self.model.eval()
        loss_sum, total = 0.0, 0
        for start in range(0, len(rows), batch_size):
            if stop.is_set():
                return None
            batch_rows = rows[start:start + batch_size]
            count = sum(len(row) - 1 for row in batch_rows)
            loss = self.loss(self.model, *self.batch(batch_rows))
            self.mx.eval(loss)
            loss_sum += loss.item() * count
            total += count
        return loss_sum / total

    def memory_metrics(self):
        return dict(gpuAllocatedBytes=self.mx.get_active_memory(), gpuPeakAllocatedBytes=self.mx.get_peak_memory())

    def export(self, path):
        from mlx.utils import tree_flatten

        path.mkdir(exist_ok=True)
        self.mx.save_safetensors(str(path / "adapters.safetensors"), dict(tree_flatten(self.model.trainable_parameters())))
        atomic_write(path / "adapter_config.json", json.dumps({
            "fine_tune_type": "lora", "num_layers": len(self.model.layers),
            "lora_parameters": self.lora_parameters,
        }, indent=2).encode())
        atomic_write(path / "config.json", json.dumps(self.config, indent=2).encode())

    def checkpoint(self, root, step):
        from mlx.utils import tree_flatten

        root.mkdir(exist_ok=True)
        destination = root / f"step-{step:08d}"
        if destination.exists():
            return
        temporary = root / f"step-{step:08d}.tmp"
        temporary.mkdir()
        self.export(temporary)
        self.mx.save_safetensors(str(temporary / "optimizer-state.safetensors"), dict(tree_flatten(self.optimizer.state)))
        self.mx.savez(str(temporary / "rng-state.npz"),
                      **{f"state_{index}": value for index, value in enumerate(self.mx.random.state)})
        atomic_write(temporary / "checkpoint.json", json.dumps({
            "step": step, "backend": "mlx", "resumeSupported": False,
        }).encode())
        os.replace(temporary, destination)
        for old in sorted(path for path in root.iterdir() if path.is_dir() and not path.name.endswith(".tmp"))[:-2]:
            shutil.rmtree(old)
