/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningDiagnostics.cs
    文件功能描述：汇总微调诊断信息
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 汇总微调诊断信息

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
