/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxNcfPreviewProxyTarget.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>Internal-only target data for the NCF preview reverse proxy.</summary>
public sealed class SandboxNcfPreviewProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
}
