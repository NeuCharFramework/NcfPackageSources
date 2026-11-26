/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxSessionInfo.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.0 0.3.0 feat: enhance Sandbox with external JupyterLab control, port mappings, session alias and raised quota

    Modified by: Senparc - 20261103
    Change description: v0.3.1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.2 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// Read-only session snapshot returned by Sandbox orchestration APIs.
/// </summary>
public sealed class SandboxSessionInfo
{
    public string SessionId { get; init; } = string.Empty;
    public int OwnerUserId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public SandboxRuntimeKind RuntimeKind { get; init; }
    public SandboxSessionStatus Status { get; init; }
    public string? AccessUrl { get; init; }
    public string? StatusMessage { get; init; }
    public int? HostPort { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public bool IsTtlUnlimited { get; init; }
    public DateTimeOffset LastActivityAtUtc { get; init; }
    /// <summary>Optional administrator-facing session alias.</summary>
    public string? Alias { get; init; }
    /// <summary>Normalized additional port mappings requested for the session.</summary>
    public string? ExtraPorts { get; init; }
}
