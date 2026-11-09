/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ConnectRetryHandler.cs
    文件功能描述：NeuCharBox Live NeuChar 模型客户端：ConnectRetryHandler


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.NeuChar;

/// <summary>
/// Retries a request only when no connection was made: DNS, TCP connect, the TLS handshake, or the connect timeout
/// (SocketsHttpHandler.ConnectTimeout covers connect and handshake). About half the TLS handshakes from Leo's PC to
/// neuchar.com stall, so these are expected. Anything else (a response of any status, a reset mid-request, the
/// caller's cancel or HttpClient.Timeout) is passed through on the first attempt: a request that may have reached the
/// server is never sent twice. Waits 0.5 s, 1 s, 2 s between attempts. It logs nothing, so headers can't leak.
/// </summary>
/// <param name="maxRetries">Retries after the first attempt.</param>
/// <param name="delay">The wait between attempts; Task.Delay when null. Tests pass a recorder so nothing sleeps.</param>
public sealed class ConnectRetryHandler(int maxRetries = 3, Func<TimeSpan, CancellationToken, Task>? delay = null) : DelegatingHandler
{
    readonly int _maxRetries = Math.Max(0, maxRetries);
    readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;

    /// <summary>The wait before retry n (1-based): 0.5 s, 1 s, 2 s, then 2 s again.</summary>
    public static TimeSpan Backoff(int retry) => TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(retry, 3) - 1));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var retry = 1; ; retry++)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (retry <= _maxRetries && NoConnection(e, cancellationToken))
            {
                await _delay(Backoff(retry), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>True only for failures that happen before a connection exists, so the request was never sent.</summary>
    internal static bool NoConnection(Exception e, CancellationToken ct) => e switch
    {
        HttpRequestException h => h.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError
            or HttpRequestError.NameResolutionError,
        // SocketsHttpHandler's ConnectTimeout: a TaskCanceledException over a TimeoutException while the caller's token
        // (which HttpClient.Timeout and the runner's budget cancel) is still live.
        TaskCanceledException { InnerException: TimeoutException } => !ct.IsCancellationRequested,
        _ => false,
    };
}
