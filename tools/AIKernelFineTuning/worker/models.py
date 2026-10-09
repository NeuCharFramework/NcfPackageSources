import json
import re

from .config import WorkerError, identifier, safe_path, validate_tree

TORCH_TYPES = {"gpt2", "llama", "mistral", "qwen2", "qwen3", "phi3", "gemma", "gemma2", "gemma3_text", "opt"}
MLX_TYPES = {"llama", "mistral", "qwen2", "qwen3", "phi3", "gemma", "gemma2"}


def model_info(root, model_id):
    path = safe_path(root, model_id)
    if not path.is_dir():
        raise WorkerError("Local model not found.", 404)
    validate_tree(path)
    try:
        config = json.loads((path / "config.json").read_text())
        tokenizer_config = json.loads((path / "tokenizer_config.json").read_text())
    except (OSError, ValueError) as exc:
        raise WorkerError("Model requires valid config.json and tokenizer_config.json.") from exc
    if not isinstance(config, dict) or not isinstance(tokenizer_config, dict):
        raise WorkerError("Model and tokenizer configuration must be JSON objects.")
    if config.get("auto_map") or tokenizer_config.get("auto_map"):
        raise WorkerError("Models/tokenizers requiring custom or remote code are forbidden.")
    if not re.fullmatch(r"[a-z0-9_]+", str(config.get("model_type", ""))):
        raise WorkerError("Invalid model_type.")
    weights = list(path.glob("model*.safetensors"))
    if not weights:
        raise WorkerError("Local model requires safetensors weights; pickle/bin weights are forbidden.")
    # Sharded index references may not escape the model directory.
    for index_path in path.glob("*.safetensors.index.json"):
        try:
            index = json.loads(index_path.read_text())
            for filename in index["weight_map"].values():
                weight = safe_path(path, filename)
                if not filename.endswith(".safetensors") or not weight.is_file():
                    raise WorkerError("Invalid or missing safetensors shard.")
        except (OSError, ValueError, KeyError, TypeError, AttributeError) as exc:
            raise WorkerError("Invalid safetensors shard index.") from exc
    quantized_mlx = bool(config.get("quantization"))
    backends = []
    if config["model_type"] in TORCH_TYPES and not quantized_mlx and not config.get("quantization_config"):
        backends += ["cpu", "cuda"]
    if config["model_type"] in MLX_TYPES and not config.get("quantization_config"):
        backends.append("mlx")
    return path, config, backends


def list_models(root):
    models = []
    rejected = []
    for path in sorted(root.iterdir()):
        if not path.is_dir():
            continue
        try:
            identifier(path.name)
            _, _, backends = model_info(root, path.name)
            if backends:
                models.append(dict(id=path.name, name=path.name, backends=backends))
        except WorkerError as exc:
            rejected.append((path.name, str(exc)))
    return models, rejected


def validate_model_request(root, request):
    path, config, backends = model_info(root, request["modelId"])
    backend, method = request["backend"], request["method"]
    if backend not in backends:
        raise WorkerError(f"Model format/architecture is incompatible with {backend}. Supported model backends: {backends}.")
    if backend == "cpu" and method == "qlora":
        raise WorkerError("CPU supports LoRA only; QLoRA requires CUDA or native Apple Silicon MLX.")
    if backend == "mlx":
        quantization = config.get("quantization")
        if method == "lora" and quantization:
            raise WorkerError("MLX LoRA requires unquantized weights; use qlora for quantized weights.")
        if method == "qlora" and quantization and (
            quantization.get("bits") != 4 or quantization.get("mode", "affine") != "affine"
        ):
            raise WorkerError("MLX QLoRA requires affine 4-bit local weights or an unquantized model.")
    maximum = config.get("max_position_embeddings", config.get("n_positions"))
    if isinstance(maximum, int) and request["maxSequenceLength"] > maximum:
        raise WorkerError(f"maxSequenceLength exceeds this model's context limit ({maximum}).")
    return path, config
