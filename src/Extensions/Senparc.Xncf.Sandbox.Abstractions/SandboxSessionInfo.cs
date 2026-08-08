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
    public DateTimeOffset LastActivityAtUtc { get; init; }
}
