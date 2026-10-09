import re
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator

from .config import WorkerError, identifier


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True, allow_inf_nan=False)


class DatasetRequest(StrictModel):
    name: str = Field(min_length=1, max_length=100)
    content: str

    @field_validator("name")
    @classmethod
    def name_valid(cls, value):
        if not value.strip() or any(ord(char) < 32 for char in value):
            raise ValueError("name must be nonblank text without control characters.")
        return value


class JobRequest(StrictModel):
    name: str = Field(min_length=1, max_length=100)
    modelId: str
    datasetId: str
    evalDatasetId: str | None = None
    backend: Literal["cpu", "cuda", "mlx"] = "cpu"
    method: Literal["lora", "qlora"] = "lora"
    epochs: int = Field(default=1, ge=1, le=100)
    maxSteps: int = Field(default=10, ge=0, le=1000000)
    learningRate: float = Field(default=0.0002, ge=0.00000001, le=0.1)
    batchSize: int = Field(default=1, ge=1, le=64)
    gradientAccumulationSteps: int = Field(default=1, ge=1, le=1024)
    maxSequenceLength: int = Field(default=256, ge=32, le=32768)
    loraRank: int = Field(default=8, ge=1, le=256)
    loraAlpha: int = Field(default=16, ge=1, le=1024)
    loraDropout: float = Field(default=0.05, ge=0, le=0.9)
    targetModules: str = Field(default="all-linear", min_length=1, max_length=512)
    warmupRatio: float = Field(default=0.03, ge=0, le=0.5)
    weightDecay: float = Field(default=0, ge=0, le=1)
    loggingSteps: int = Field(default=1, ge=1, le=10000)
    saveSteps: int = Field(default=20, ge=1, le=100000)
    evalSteps: int = Field(default=20, ge=1, le=100000)
    seed: int = Field(default=42, ge=0, le=2147483647)
    maxDurationMinutes: int = Field(default=60, ge=1, le=10080)

    @field_validator("name")
    @classmethod
    def name_valid(cls, value):
        return DatasetRequest.name_valid(value)

    @field_validator("modelId", "datasetId", "evalDatasetId")
    @classmethod
    def identifier_valid(cls, value):
        if value is not None:
            try:
                identifier(value)
            except WorkerError as exc:
                raise ValueError(str(exc)) from exc
        return value

    @field_validator("targetModules")
    @classmethod
    def targets_valid(cls, value):
        if value != "all-linear" and not re.fullmatch(r"[A-Za-z0-9_.]+(?:,[A-Za-z0-9_.]+)*", value):
            raise ValueError("targetModules must be all-linear or comma-separated literal module names (not regex).")
        return value
