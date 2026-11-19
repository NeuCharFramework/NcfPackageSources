/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxJupyterProxyTarget.cs
    File description: SandboxJupyterProxyTarget related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// For server-side proxy use only; do not send to the browser.
/// </summary>
public sealed class SandboxJupyterProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
    public required string AccessToken { get; init; }
}
