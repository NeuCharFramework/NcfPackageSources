namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>Internal-only target data for the NCF preview reverse proxy.</summary>
public sealed class SandboxNcfPreviewProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
}
