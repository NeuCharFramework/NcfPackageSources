/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNcfPreviewOptions.cs
    文件功能描述：固定 NCF 预览容器的安全配置

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0-preview3 增加 NCF 预览沙箱工作负载

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// Configuration section: SenparcXncfSandbox:NcfPreview.
/// <para>
/// The preview image must be supplied through Images:Overrides:ncf-preview and pinned by digest.
/// The default is disabled, and dependency restore egress is disabled even when the workload is
/// enabled. An organisation that enables restore must point at a restricted package-mirror network.
/// </para>
/// </summary>
public sealed class SandboxNcfPreviewOptions
{
    public const string SectionName = "SenparcXncfSandbox:NcfPreview";

    public bool Enabled { get; set; }
    public bool AllowDependencyRestoreNetwork { get; set; }
    public string? RestoreNetworkName { get; set; }
    public int StartupTimeoutSeconds { get; set; } = 180;
}
