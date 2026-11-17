/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxTemplateKeys.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260815
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 Updated module behavior and related support.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public static class SandboxTemplateKeys
{
    public const string PythonExec = "python-exec";
    public const string CsharpExec = "csharp-exec";
    public const string JupyterPython = "jupyter-python";
    public const string JupyterCsharp = "jupyter-csharp";
    /// <summary>
    /// Dedicated NCF/XNCF preview workload. It is not a generic shell and has a fixed build and
    /// launch sequence.
    /// </summary>
    public const string NcfPreview = "ncf-preview";
}
