/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningMigrationResult.cs
    文件功能描述：描述微调迁移结果
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 描述微调迁移结果

----------------------------------------------------------------*/

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
