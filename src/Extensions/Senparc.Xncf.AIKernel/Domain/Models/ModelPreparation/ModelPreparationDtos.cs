/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ModelPreparationDtos.cs
    文件功能描述：定义模型准备传输对象
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 补充模型准备传输对象

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;

public sealed record ModelPreparationRequest
{
    public string Provider { get; init; } = "";
    public string Model { get; init; } = "";
    public string Revision { get; init; } = "";
    public string TargetDirectory { get; init; } = "";
}

public sealed record ModelFileStatus(string Path, long Length, bool Exists);

public sealed record ModelPreparationStatus
{
    public string Provider { get; init; } = "";
    public string Model { get; init; } = "";
    public string Revision { get; init; } = "";
    public string TargetDirectory { get; init; } = "";
    public bool IsComplete { get; init; }
    public long TotalBytes { get; init; }
    public IReadOnlyList<ModelFileStatus> Files { get; init; } = Array.Empty<ModelFileStatus>();
}

public sealed record ModelPreparationResult
{
    public ModelPreparationStatus Status { get; init; } = new();
    public bool Downloaded { get; init; }
}

