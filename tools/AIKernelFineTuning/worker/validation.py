import json
from pathlib import Path

from .storage import atomic_write, utc_now


def _torch_device_and_dtype(torch, backend: str):
    device = "cuda" if backend == "cuda" else "cpu"
    if device == "cuda" and not torch.cuda.is_available():
        raise RuntimeError("CUDA validation was selected, but torch.cuda.is_available() is false.")
    dtype = torch.float32 if device == "cpu" else (
        torch.bfloat16 if torch.cuda.is_bf16_supported() else torch.float16
    )
    return device, dtype


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
                max_tokens = available_tokens(tokenizer, prompt, request.maxTokens)
                return generate(model, tokenizer, prompt=prompt, max_tokens=max_tokens)
        else:
            import torch
            from peft import PeftModel
            from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig

            device, dtype = _torch_device_and_dtype(torch, backend)
            tokenizer = AutoTokenizer.from_pretrained(
                model_path, local_files_only=True, trust_remote_code=False)
            options = dict(
                local_files_only=True,
                trust_remote_code=False,
                use_safetensors=True,
                torch_dtype=dtype,
            )
            if method == "qlora":
                if device != "cuda":
                    raise RuntimeError("PyTorch QLoRA validation requires CUDA backend.")
                options.update(
                    device_map={"": 0},
                    quantization_config=BitsAndBytesConfig(
                        load_in_4bit=True,
                        bnb_4bit_quant_type="nf4",
                        bnb_4bit_use_double_quant=True,
                        bnb_4bit_compute_dtype=dtype,
                    ),
                )
            model = AutoModelForCausalLM.from_pretrained(model_path, **options)
            if method != "qlora":
                model = model.to(device)
            model = PeftModel.from_pretrained(
                model, adapter_path, local_files_only=True).eval()

            def generate_response(prompt):
                inputs = tokenizer(prompt, return_tensors="pt").to(device)
                max_tokens = available_tokens(tokenizer, prompt, request.maxTokens)
                with torch.no_grad():
                    output = model.generate(**inputs, max_new_tokens=max_tokens, do_sample=False)
                return tokenizer.decode(output[0], skip_special_tokens=True)

        for case in request.cases:
            result = {
                "id": case.id,
                "prompt": case.prompt,
                "response": generate_response(case.prompt)[:131072],
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


def available_tokens(tokenizer, prompt, requested):
    # mlx-lm TokenizerWrapper forwards encode() but is not callable.
    encoded = tokenizer.encode(prompt, add_special_tokens=True, truncation=False)
    input_tokens = len(encoded)
    context_limit = getattr(tokenizer, "model_max_length", 0)
    if not isinstance(context_limit, int) or context_limit <= 0 or context_limit > 1_000_000:
        return requested
    available = context_limit - input_tokens
    if available < 16:
        raise RuntimeError(
            f"Prompt uses {input_tokens} tokens, leaving fewer than 16 tokens in the model context window ({context_limit}).")
    return min(requested, available)
