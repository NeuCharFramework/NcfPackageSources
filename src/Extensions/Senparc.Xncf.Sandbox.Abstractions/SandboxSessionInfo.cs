/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SandboxSessionInfo.cs
    文件功能描述：跨模块可共享的沙箱会话快照
    
    
    创建标识：Senparc - 20260808
    
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增加 IsTtlUnlimited 契约字段

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
}
