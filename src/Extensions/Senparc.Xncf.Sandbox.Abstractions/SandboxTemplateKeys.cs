namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// 预置模板键（协议值，勿本地化）。
/// </summary>
public static class SandboxTemplateKeys
{
    public const string PythonExec = "python-exec";
    public const string CsharpExec = "csharp-exec";
    public const string JupyterPython = "jupyter-python";
    /// <summary>
    /// Dedicated NCF/XNCF preview workload. It is not a generic shell and has a fixed build and
    /// launch sequence.
    /// </summary>
    public const string NcfPreview = "ncf-preview";
}
