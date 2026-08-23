/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IXncfSandboxPreviewService.cs
    文件功能描述：跨模块 NCF 预览沙箱启动、停止与状态契约

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱跨模块契约

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 同步沙箱会话 TTL 契约语义

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 扩展沙箱 Jupyter 与会话生命周期契约

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// Cross-module contract for a fixed-function NCF preview workload. XncfBuilder references only
/// this assembly, so the builder can fail closed when the optional Sandbox module is not installed.
/// </summary>
public interface IXncfSandboxPreviewService
{
    Task<XncfSandboxPreviewInfo> StartAsync(
        XncfSandboxPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task StopAsync(string sandboxSessionId, CancellationToken cancellationToken = default);
}

public sealed class XncfSandboxPreviewRequest
{
    /// <summary>Sanitized, isolated source snapshot; never a production checkout.</summary>
    public string SourceWorkspacePath { get; init; } = string.Empty;
    /// <summary>Path to the solution relative to <see cref="SourceWorkspacePath"/>.</summary>
    public string SolutionRelativePath { get; init; } = string.Empty;
    public string ModuleProjectName { get; init; } = string.Empty;
    public int OwnerUserId { get; init; }
    /// <summary>
    /// Disabled by default. A Sandbox administrator must separately configure an approved,
    /// restricted package mirror network before this can ever be enabled.
    /// </summary>
    public bool AllowDependencyRestoreNetwork { get; init; }
}

public sealed class XncfSandboxPreviewInfo
{
    public string SandboxSessionId { get; init; } = string.Empty;
    public SandboxSessionStatus Status { get; init; }
    public string? AccessUrl { get; init; }
    public string? StatusMessage { get; init; }
}
