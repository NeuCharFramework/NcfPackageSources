using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

public sealed class FineTuningMigrationResult
{
    public string WorkerAlias { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string Status { get; set; } = "";
    public int Datasets { get; set; }
    public int Jobs { get; set; }
    public int Events { get; set; }
    public int Artifacts { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? Error { get; set; }
}
