/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxImageOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class SandboxImageOptions
{
    public const string SectionName = "SenparcXncfSandbox:Images";

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public string? RegistryPrefix { get; set; }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public Dictionary<string, string> Overrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
