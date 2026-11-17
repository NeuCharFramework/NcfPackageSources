namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class SandboxJupyterProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
    public required string AccessToken { get; init; }
}
