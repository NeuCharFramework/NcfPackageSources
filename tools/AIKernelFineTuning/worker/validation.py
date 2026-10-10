import json
from pathlib import Path

from .storage import atomic_write, utc_now


def run_validation(root: Path, model_path: Path, adapter_path: Path, request, emit):
    from mlx_lm import generate, load

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
        model, tokenizer = load(str(model_path), adapter_path=str(adapter_path))
        for case in request.cases:
            # mlx-lm 0.28.x uses its deterministic argmax sampler by default;
            # it does not expose the older `temp` keyword.
            response = generate(model, tokenizer, prompt=case.prompt, max_tokens=request.maxTokens)
            result = {
                "id": case.id,
                "prompt": case.prompt,
                "response": response[:8192],
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
