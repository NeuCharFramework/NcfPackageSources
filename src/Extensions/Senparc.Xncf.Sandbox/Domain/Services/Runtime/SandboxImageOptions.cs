/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxImageOptions.cs
    文件功能描述：沙箱镜像仓库前缀与按模板覆盖（tag 细节以 Docs 为准）

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// 配置节：SenparcXncfSandbox:Images
/// <para>推荐镜像清单见 https://doc.ncf.pub/zh/NcfPackageSources/xncf/sandbox-environment.html</para>
/// </summary>
public sealed class SandboxImageOptions
{
    public const string SectionName = "SenparcXncfSandbox:Images";

    /// <summary>
    /// 内部仓库前缀，例如 registry.example.com/ncf-sandbox（不含末尾 /）。
    /// 未配置 Overrides 时，将与模板默认「短名」拼接。
    /// </summary>
    public string? RegistryPrefix { get; set; }

    /// <summary>
    /// 按模板键完全覆盖镜像引用（优先于 RegistryPrefix）。
    /// 键为 python-exec / csharp-exec / jupyter-python / jupyter-csharp。
    /// </summary>
    public Dictionary<string, string> Overrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
