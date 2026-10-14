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

