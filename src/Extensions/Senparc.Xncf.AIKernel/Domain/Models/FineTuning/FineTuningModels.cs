#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

public sealed class FineTuningJobRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [Required, StringLength(128)]
    public string ModelId { get; set; } = "";
    [Required, StringLength(128)]
    public string DatasetId { get; set; } = "";
    [StringLength(128)]
    public string? EvalDatasetId { get; set; }
    [Required, RegularExpression("^(cpu|cuda|mlx)$")]
    public string Backend { get; set; } = "cpu";
    [Required, RegularExpression("^(lora|qlora)$")]
    public string Method { get; set; } = "lora";
    [Range(1, 100)]
    public int Epochs { get; set; } = 1;
    [Range(0, 1000000)]
    public int MaxSteps { get; set; } = 10;
    [Range(0.00000001, 0.1)]
    public double LearningRate { get; set; } = 0.0002;
    [Range(1, 64)]
    public int BatchSize { get; set; } = 1;
    [Range(1, 1024)]
    public int GradientAccumulationSteps { get; set; } = 1;
    [Range(32, 32768)]
    public int MaxSequenceLength { get; set; } = 256;
    [Range(1, 256)]
    public int LoraRank { get; set; } = 8;
    [Range(1, 1024)]
    public int LoraAlpha { get; set; } = 16;
    [Range(0, 0.9)]
    public double LoraDropout { get; set; } = 0.05;
    [Required, StringLength(512)]
    public string TargetModules { get; set; } = "all-linear";
    [Range(0, 0.5)]
    public double WarmupRatio { get; set; } = 0.03;
    [Range(0, 1)]
    public double WeightDecay { get; set; }
    [Range(1, 10000)]
    public int LoggingSteps { get; set; } = 1;
    [Range(1, 100000)]
    public int SaveSteps { get; set; } = 20;
    [Range(1, 100000)]
    public int EvalSteps { get; set; } = 20;
    [Range(0, int.MaxValue)]
    public int Seed { get; set; } = 42;
    [Range(1, 10080)]
    public int MaxDurationMinutes { get; set; } = 60;
}

public sealed class FineTuningDatasetRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [Required]
    public string Content { get; set; } = "";
}

public sealed class FineTuningCancelRequest
{
    [Required, StringLength(128)]
    public string Id { get; set; } = "";
}

public sealed class FineTuningHealth
{
    public string? StoreId { get; set; }
    [JsonRequired]
    public string Status { get; set; } = "";
    [JsonRequired]
    public string Version { get; set; } = "";
    [JsonRequired]
    public List<FineTuningCapability> Capabilities { get; set; } = new();
    [JsonRequired]
    public int ActiveJobs { get; set; }
    [JsonRequired]
    public int QueuedJobs { get; set; }
}

public sealed class FineTuningCatalogPage<T>
{
    [JsonRequired]
    public List<T> Items { get; set; } = new();
    [JsonRequired]
    public int Total { get; set; }
    [JsonRequired]
    public int Offset { get; set; }
    [JsonRequired]
    public int Limit { get; set; }
}

public sealed class FineTuningCapability
{
    [JsonRequired]
    public string Backend { get; set; } = "";
    [JsonRequired]
    public List<string> Methods { get; set; } = new();
    [JsonRequired]
    public bool Available { get; set; }
    public string? Reason { get; set; }
}

public sealed class FineTuningModel
{
    [JsonRequired]
    public string Id { get; set; } = "";
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public List<string> Backends { get; set; } = new();
}

public sealed class FineTuningDataset
{
    [JsonRequired]
    public string Id { get; set; } = "";
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public int Rows { get; set; }
    [JsonRequired]
    public string Sha256 { get; set; } = "";
    [JsonRequired]
    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class FineTuningJob
{
    [JsonRequired]
    public string Id { get; set; } = "";
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public string State { get; set; } = "";
    [JsonRequired]
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? FinishedUtc { get; set; }
    [JsonRequired]
    public FineTuningJobRequest Request { get; set; } = new();
    public string? Error { get; set; }
    [JsonRequired]
    public List<FineTuningArtifact> Artifacts { get; set; } = new();
    [JsonRequired]
    public Dictionary<string, double?> LatestMetrics { get; set; } = new();
    [JsonRequired]
    public long LastEventSequence { get; set; }
}

public sealed class FineTuningArtifact
{
    [JsonRequired]
    public string Id { get; set; } = "";
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public long Bytes { get; set; }
}

public sealed class FineTuningEventPage
{
    [JsonRequired]
    public List<FineTuningEvent> Events { get; set; } = new();
    [JsonRequired]
    public long NextCursor { get; set; }
    [JsonRequired]
    public bool HasMore { get; set; }
    public bool Truncated { get; set; }
}

public sealed class FineTuningEvent
{
    [JsonRequired]
    public long Sequence { get; set; }
    [JsonRequired]
    public DateTimeOffset TimestampUtc { get; set; }
    [JsonRequired]
    public string Kind { get; set; } = "";
    public string? Message { get; set; }
    [JsonRequired]
    public Dictionary<string, double?> Metrics { get; set; } = new();
}
