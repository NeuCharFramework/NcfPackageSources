/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningDiagnostics.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

----------------------------------------------------------------*/

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
