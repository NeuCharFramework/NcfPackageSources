import json
from pathlib import Path

from .storage import atomic_write, utc_now


def run_validation(root: Path, model_path: Path, adapter_path: Path, request, emit,
                   backend: str, method: str):
    state_path = root / "validation.json"
    state = {
        "status": "Running",
        "startedUtc": utc_now(),
        "finishedUtc": None,
        "completed": 0,
        "total": len(request.cases),
        "results": [],
        "error": None,
    }

    def save():
        atomic_write(state_path, json.dumps(state, ensure_ascii=False, indent=2).encode())

    save()
    try:
        if backend == "mlx":
            from mlx_lm import generate, load

            model, tokenizer = load(str(model_path), adapter_path=str(adapter_path))

            def generate_response(prompt):
                # mlx-lm 0.28.x uses deterministic argmax by default.
                return generate(model, tokenizer, prompt=prompt, max_tokens=request.maxTokens)
        else:
            if method == "qlora":
                raise RuntimeError("PyTorch QLoRA validation requires the matching quantization runtime and is not enabled by this validator.")
            import torch
            from peft import PeftModel
            from transformers import AutoModelForCausalLM, AutoTokenizer

            device = "cuda" if backend == "cuda" else "cpu"
            if device == "cuda" and not torch.cuda.is_available():
                raise RuntimeError("CUDA validation was selected, but torch.cuda.is_available() is false.")
            tokenizer = AutoTokenizer.from_pretrained(
                model_path, local_files_only=True, trust_remote_code=False)
            model = AutoModelForCausalLM.from_pretrained(
                model_path, local_files_only=True, trust_remote_code=False,
                use_safetensors=True, torch_dtype="auto").to(device)
            model = PeftModel.from_pretrained(
                model, adapter_path, local_files_only=True).eval()

            def generate_response(prompt):
                inputs = tokenizer(prompt, return_tensors="pt").to(device)
                with torch.no_grad():
                    output = model.generate(**inputs, max_new_tokens=request.maxTokens, do_sample=False)
                return tokenizer.decode(output[0], skip_special_tokens=True)

        for case in request.cases:
            result = {
                "id": case.id,
                "prompt": case.prompt,
                "response": generate_response(case.prompt)[:8192],
                "completedUtc": utc_now(),
            }
            state["results"].append(result)
            state["completed"] += 1
            emit("log", f"Validation case {case.id} completed.", {"validationCompleted": state["completed"]})
            save()
        state["status"] = "Succeeded"
    except Exception as exc:
        state["status"] = "Failed"
        state["error"] = str(exc)[:2048]
        emit("error", "Validation failed.", {})
    finally:
        state["finishedUtc"] = utc_now()
        save()
