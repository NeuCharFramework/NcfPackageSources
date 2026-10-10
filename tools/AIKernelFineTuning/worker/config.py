import os
import re
from dataclasses import dataclass
from pathlib import Path

IDENTIFIER = re.compile(r"[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}\Z")
MAX_DATASET_BYTES = 2 * 1024 * 1024
OFFLINE_ENV = {
    "HF_HUB_OFFLINE": "1",
    "TRANSFORMERS_OFFLINE": "1",
    "HF_DATASETS_OFFLINE": "1",
    "HF_HUB_DISABLE_TELEMETRY": "1",
    "TOKENIZERS_PARALLELISM": "false",
    "WANDB_DISABLED": "true",
    "DO_NOT_TRACK": "1",
}


class WorkerError(Exception):
    def __init__(self, message, status=422, details=None):
        super().__init__(message)
        self.status = status
        self.details = details


def identifier(value):
    if not isinstance(value, str) or not IDENTIFIER.fullmatch(value) or ".." in value:
        raise WorkerError("Invalid resource identifier.")
    return value


def safe_path(root, *parts):
    path = root.joinpath(*(identifier(part) for part in parts))
    # Reject symlinks even when they happen to point inside the allowed root.
    current = root
    for part in parts:
        current = current / part
        if current.is_symlink():
            raise WorkerError("Symlinks are not allowed in worker resources.")
    if not path.resolve().is_relative_to(root.resolve()):
        raise WorkerError("Resource escapes its allowed root.")
    return path


def validate_tree(path):
    if path.is_symlink():
        raise WorkerError("Model symlinks are not allowed.")
    for entry in path.rglob("*"):
        if entry.is_symlink() or not (entry.is_file() or entry.is_dir()):
            raise WorkerError("Models must contain only regular files and directories.")


@dataclass(frozen=True)
class Settings:
    key: str
    models: Path
    data: Path
    queue_limit: int = 8
    event_limit: int = 10000
    cancel_grace_seconds: float = 30
    telemetry_seconds: float = 2
    preflight_seconds: float = 120
    torch_threads: int = 2
    max_duration_minutes: int = 1440
    max_job_bytes: int = 20 * 1024**3
    min_free_bytes: int = 1024**3
    max_datasets: int = 1000
    max_jobs: int = 1000

    @classmethod
    def from_env(cls):
        key = os.environ.get("NCF_WORKER_KEY", "")
        if len(key) < 32 or key != key.strip() or "\r" in key or "\n" in key:
            raise RuntimeError("NCF_WORKER_KEY is required (at least 32 characters, no surrounding whitespace or line breaks).")
        models = Path(os.environ.get("NCF_MODELS_ROOT", "/models")).absolute()
        data = Path(os.environ.get("NCF_DATA_ROOT", "/data")).absolute()
        if models.is_symlink() or data.is_symlink():
            raise RuntimeError("Worker roots must not be symlinks.")
        if not models.is_dir():
            raise RuntimeError("NCF_MODELS_ROOT must be an existing local directory.")
        if models.resolve().is_relative_to(data.resolve()) or data.resolve().is_relative_to(models.resolve()):
            raise RuntimeError("Models and writable data roots must not overlap.")
        return cls(
            key, models.resolve(), data.resolve(),
            queue_limit=env_int("NCF_QUEUE_LIMIT", 8, 1, 100),
            event_limit=env_int("NCF_EVENT_LIMIT", 10000, 200, 100000),
            cancel_grace_seconds=env_int("NCF_CANCEL_GRACE_SECONDS", 30, 1, 300),
            telemetry_seconds=env_int("NCF_TELEMETRY_SECONDS", 2, 1, 60),
            preflight_seconds=env_int("NCF_PREFLIGHT_SECONDS", 120, 10, 600),
            torch_threads=env_int("NCF_TORCH_THREADS", 2, 1, 64),
            max_duration_minutes=env_int("NCF_MAX_DURATION_MINUTES", 1440, 1, 10080),
            max_job_bytes=env_int("NCF_MAX_JOB_BYTES", 20 * 1024**3, 1024**2, 1024**4),
            min_free_bytes=env_int("NCF_MIN_FREE_BYTES", 1024**3, 1024**2, 1024**4),
            max_datasets=env_int("NCF_MAX_DATASETS", 1000, 1, 100000),
            max_jobs=env_int("NCF_MAX_JOBS", 1000, 1, 100000),
        )


def env_int(name, default, minimum, maximum):
    try:
        value = int(os.environ.get(name, str(default)))
    except ValueError as exc:
        raise RuntimeError(f"{name} must be an integer.") from exc
    if not minimum <= value <= maximum:
        raise RuntimeError(f"{name} must be between {minimum} and {maximum}.")
    return value
