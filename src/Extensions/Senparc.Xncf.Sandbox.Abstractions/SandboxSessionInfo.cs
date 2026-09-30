/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SandboxSessionInfo.cs
    文件功能描述：跨模块可共享的沙箱会话快照
    
    
    创建标识：Senparc - 20260808
    
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增加 IsTtlUnlimited 契约字段

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 扩展沙箱 Jupyter 与会话生命周期契约

    修改标识：Senparc - 20260918
    修改描述：v0.3.3 增加 Alias 与 ExtraPorts 契约字段

    修改标识：Senparc - 20260918
    修改描述：v0.3.3 增加 Alias 与 ExtraPorts 契约字段

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Abstractions;

/// <summary>
/// 跨模块可共享的沙箱会话快照。
/// </summary>
public sealed class SandboxSessionInfo
{
    public string SessionId { get; init; } = string.Empty;
    public int OwnerUserId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public SandboxRuntimeKind RuntimeKind { get; init; }
    public SandboxSessionStatus Status { get; init; }
    public string? AccessUrl { get; init; }
    public string? StatusMessage { get; init; }
    public int? HostPort { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public bool IsTtlUnlimited { get; init; }
    public DateTimeOffset LastActivityAtUtc { get; init; }
    /// <summary>
    /// 可选的会话别名，便于在列表和日志中识别（非协议值，可本地化展示）。
    /// </summary>
    public string? Alias { get; init; }
    /// <summary>
    /// 创建时解析后的附加端口映射展示串（hostPort:containerPort 以分号分隔），无则为空。
    /// </summary>
    public string? ExtraPorts { get; init; }
}
