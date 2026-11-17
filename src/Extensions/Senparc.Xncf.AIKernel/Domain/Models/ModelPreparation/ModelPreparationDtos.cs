/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ModelPreparationDtos.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add PromptRange selector service, UI components, and tests

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

public sealed record ModelSearchRequest
{
    public string Provider { get; init; } = "";
    public string Query { get; init; } = "";
    public int Limit { get; init; } = 20;
}

public sealed record ModelSearchItem
{
    public string Provider { get; init; } = "";
    public string Model { get; init; } = "";
    public string Revision { get; init; } = "";
    public string Description { get; init; } = "";
    public long SizeBytes { get; init; }
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
