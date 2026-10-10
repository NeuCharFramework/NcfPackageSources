"""Verify that an exported PEFT adapter contains updates and can run inference."""

import io
import hashlib
import json
import tempfile
import zipfile
from pathlib import Path


def _extract_export(payload, model_path, root):
    with zipfile.ZipFile(io.BytesIO(payload)) as archive:
        manifest = json.loads(archive.read("manifest.json"))
        assert manifest["baseModelSha256"], "Missing base-model provenance."
        for name, expected in manifest["baseModelSha256"].items():
            with (Path(model_path) / name).open("rb") as stream:
                assert hashlib.file_digest(stream, "sha256").hexdigest() == expected
        for entry in archive.infolist():
            if not entry.filename.startswith(("adapter/", "tokenizer/")) or entry.is_dir():
                continue
            destination = root / entry.filename
            if not destination.resolve().is_relative_to(root.resolve()):
                raise AssertionError("Export entry escapes the test directory.")
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(archive.read(entry))


def verify_torch_export(payload, model_path):
    import torch
    from peft import PeftModel
    from safetensors.torch import load_file
    from transformers import AutoModelForCausalLM, AutoTokenizer

    with tempfile.TemporaryDirectory(prefix="ncf-adapter-reload-") as temporary:
        root = Path(temporary)
        _extract_export(payload, model_path, root)
        tensors = load_file(root / "adapter" / "adapter_model.safetensors")
        updates = [value for name, value in tensors.items() if "lora_B" in name]
        assert updates and all(torch.isfinite(value).all().item() for value in tensors.values())
        assert any(torch.count_nonzero(value).item() > 0 for value in updates), "Adapter B matrices were never updated."
        tokenizer = AutoTokenizer.from_pretrained(root / "tokenizer", local_files_only=True, trust_remote_code=False)
        model = AutoModelForCausalLM.from_pretrained(model_path, local_files_only=True,
                                                   trust_remote_code=False, use_safetensors=True)
        model.eval()
        inputs = tokenizer("Question 0", return_tensors="pt")
        with torch.no_grad():
            base_logits = model(**inputs).logits.clone()
            tuned = PeftModel.from_pretrained(model, root / "adapter", local_files_only=True)
            tuned.eval()
            tuned_logits = tuned(**inputs).logits
        assert torch.isfinite(tuned_logits).all().item()
        delta = (tuned_logits - base_logits).abs().max().item()
        assert delta > 0, "Reloading the adapter did not change model output."
        return dict(adapterReloadVerified=True, maxLogitDelta=delta)


def verify_mlx_export(payload, model_path):
    import mlx.core as mx
    import mlx.nn as nn
    from mlx_lm.tuner.utils import load_adapters
    from mlx_lm.utils import load_model
    from transformers import AutoTokenizer

    with tempfile.TemporaryDirectory(prefix="ncf-mlx-adapter-reload-") as temporary:
        root = Path(temporary)
        _extract_export(payload, model_path, root)
        tensors = mx.load(str(root / "adapter" / "adapters.safetensors"))
        updates = [value for name, value in tensors.items() if "lora_b" in name]
        assert updates and all(mx.all(mx.isfinite(value)).item() for value in tensors.values())
        assert any(mx.any(value != 0).item() for value in updates), "MLX adapter B matrices were never updated."
        model, config = load_model(Path(model_path), lazy=False, strict=True)
        exported = json.loads((root / "adapter" / "config.json").read_text())
        if exported.get("quantization") and not config.get("quantization"):
            nn.quantize(model, **exported["quantization"])
        tokenizer = AutoTokenizer.from_pretrained(root / "tokenizer", local_files_only=True, trust_remote_code=False)
        tokens = mx.array([tokenizer.encode("Question 0", add_special_tokens=True)])
        model.eval()
        base_logits = model(tokens)
        mx.eval(base_logits)
        load_adapters(model, root / "adapter")
        model.eval()
        tuned_logits = model(tokens)
        assert mx.all(mx.isfinite(tuned_logits)).item()
        delta = mx.max(mx.abs(tuned_logits - base_logits)).item()
        assert delta > 0, "Reloading the MLX adapter did not change model output."
        return dict(adapterReloadVerified=True, maxLogitDelta=delta)
