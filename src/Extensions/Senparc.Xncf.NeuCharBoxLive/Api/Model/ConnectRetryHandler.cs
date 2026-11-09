/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ConnectRetryHandler.cs
    文件功能描述：NeuCharBox Live 模型调用：ConnectRetryHandler（连接失败与响应前连接重置的重试）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 从 NeuChar/ 移入 Api/Model，并加入响应前连接重置（10054/10053）的重试（ncb/live fix/model-call-retry 90321e1）

----------------------------------------------------------------*/

using System.Net.Sockets;

namespace Senparc.Xncf.NeuCharBoxLive.Api.Model;

/// <summary>
/// Retries two kinds of failure, each with its own count:
/// <list type="bullet">
/// <item>No connection was made: DNS, TCP connect, the TLS handshake, or the connect timeout (SocketsHttpHandler.ConnectTimeout
/// covers connect and handshake). About half the TLS handshakes from Leo's PC to neuchar.com stall, so these are expected.
/// Up to <c>maxRetries</c>.</item>
/// <item>The connection was reset (10054) or aborted (10053) before a response came: SocketsHttpHandler threw from SendAsync,
/// so no status line and headers were parsed and nothing reached the caller (2026-11-08, mid-call resets from neuchar.com).
/// Up to <c>resetRetries</c>. The request had been sent, so the model service may have seen it twice; that is the price of
/// not failing the whole visitor request.</item>
/// </list>
/// Anything else (a response of any status, a reset once the response had started, a clean close, the caller's cancel or
/// HttpClient.Timeout) is passed through on the first attempt. A reset after the response started can't reach this handler:
/// the body is read after SendAsync returns. Waits 0.5 s, 1 s, 2 s between attempts. It logs nothing, so headers can't leak.
/// </summary>
/// <param name="maxRetries">Retries after a failed connect.</param>
/// <param name="delay">The wait between attempts; Task.Delay when null. Tests pass a recorder so nothing sleeps.</param>
/// <param name="resetRetries">Retries after a reset before the response.</param>
public sealed class ConnectRetryHandler(int maxRetries = 3, Func<TimeSpan, CancellationToken, Task>? delay = null, int resetRetries = 2)
    : DelegatingHandler
{
    readonly int _maxRetries = Math.Max(0, maxRetries);
    readonly int _resetRetries = Math.Max(0, resetRetries);
    readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;

    /// <summary>The wait before retry n (1-based): 0.5 s, 1 s, 2 s, then 2 s again.</summary>
    public static TimeSpan Backoff(int retry) => TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(retry, 3) - 1));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int connects = 0, resets = 0;
        while (true)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (connects < _maxRetries && NoConnection(e, cancellationToken))
            {
                await _delay(Backoff(++connects), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (resets < _resetRetries && ResetBeforeResponse(e, cancellationToken))
            {
                await _delay(Backoff(++resets), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>True only for failures that happen before a connection exists, so the request was never sent.</summary>
    internal static bool NoConnection(Exception e, CancellationToken ct) => e switch
    {
        HttpRequestException h => IsConnectError(h),
        // SocketsHttpHandler's ConnectTimeout: a TaskCanceledException over a TimeoutException while the caller's token
        // (which HttpClient.Timeout and the runner's budget cancel) is still live.
        TaskCanceledException { InnerException: TimeoutException } => !ct.IsCancellationRequested,
        _ => false,
    };

    /// <summary>
    /// SocketsHttpHandler's shape for a reset or abort once connected and before the response: an HttpRequestException with
    /// no status code over the read's IOException over the SocketException. A reset during connect or handshake is a
    /// connect failure and keeps the connect count. Never once the caller's token is cancelled.
    /// </summary>
    internal static bool ResetBeforeResponse(Exception e, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || e is not HttpRequestException { StatusCode: null } h || IsConnectError(h)) return false;
        for (var x = h.InnerException; x is not null; x = x.InnerException)
            if (x is SocketException { SocketErrorCode: SocketError.ConnectionReset or SocketError.ConnectionAborted }) return true;
        return false;
    }

    static bool IsConnectError(HttpRequestException h) =>
        h.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError or HttpRequestError.NameResolutionError;
}
