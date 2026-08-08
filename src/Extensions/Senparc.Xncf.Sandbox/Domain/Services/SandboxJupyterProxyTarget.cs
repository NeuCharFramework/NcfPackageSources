namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// 仅供服务端代理使用；勿下发给浏览器。
/// </summary>
public sealed class SandboxJupyterProxyTarget
{
    public required string SessionId { get; init; }
    public required int HostPort { get; init; }
    public required string AccessToken { get; init; }
}
