/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxDockerOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// Configures Docker-specific Sandbox startup behavior.
/// </summary>
public sealed class SandboxDockerOptions
{
    public const string SectionName = "SenparcXncfSandbox:Docker";

    /// <summary>Maximum time allowed to create an interactive Docker session, in seconds.</summary>
    public int InteractiveCreateTimeoutSeconds { get; set; } = 900;

    /// <summary>
    /// Returns the configured interactive-create timeout clamped to 60 seconds through one hour.
    /// </summary>
    /// <returns>The effective interactive creation timeout.</returns>
    public TimeSpan GetInteractiveCreateTimeout() =>
        TimeSpan.FromSeconds(Math.Clamp(InteractiveCreateTimeoutSeconds, 60, 3600));
}
