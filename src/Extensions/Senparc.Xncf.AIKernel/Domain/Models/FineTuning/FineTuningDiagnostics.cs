namespace Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

public sealed class FineTuningDiagnostics
{
    public string WorkerAlias { get; set; } = "";
    public bool FineTuningEnabled { get; set; }
    public bool SecretConfigured { get; set; }
    public bool WorkerReachable { get; set; }
    public bool FileManagerAvailable { get; set; }
    public int ModelCount { get; set; }
    public string WorkerVersion { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Error { get; set; }
}
