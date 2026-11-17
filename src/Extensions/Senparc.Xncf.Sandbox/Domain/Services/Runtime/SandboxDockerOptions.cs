/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxDockerOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class SandboxDockerOptions
{
    public const string SectionName = "SenparcXncfSandbox:Docker";

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public int InteractiveCreateTimeoutSeconds { get; set; } = 900;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public TimeSpan GetInteractiveCreateTimeout() =>
        TimeSpan.FromSeconds(Math.Clamp(InteractiveCreateTimeoutSeconds, 60, 3600));
}
