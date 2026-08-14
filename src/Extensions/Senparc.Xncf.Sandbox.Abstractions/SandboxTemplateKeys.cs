/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxTemplateKeys.cs
    文件功能描述：沙箱预置模板协议键定义


    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0-preview2 增加 NCF 预览沙箱跨模块契约

----------------------------------------------------------------*/

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
