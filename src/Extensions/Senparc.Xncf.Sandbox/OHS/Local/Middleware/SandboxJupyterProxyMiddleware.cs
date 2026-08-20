/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxJupyterProxyMiddleware.cs
    文件功能描述：将已登录管理员的请求代理到本机 Jupyter（含 WebSocket）

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

using System.Net.Http.Headers;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.OHS.Local.Middleware;

/// <summary>
/// /sandbox-jupyter/{sessionId}/... → http://127.0.0.1:{port}/sandbox-jupyter/{sessionId}/...
/// 要求 NCF Admin Cookie 已登录；Jupyter token 仅在服务端注入，不出现在对外 AccessUrl。
/// </summary>
public sealed class SandboxJupyterProxyMiddleware
{
    public const string HttpClientName = "Senparc.Xncf.Sandbox.JupyterProxy";

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailers", "Transfer-Encoding", "Upgrade", "Host", "Content-Length"
    };

    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SandboxOrchestrator _orchestrator;
    private readonly ILogger<SandboxJupyterProxyMiddleware> _logger;

    public SandboxJupyterProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        SandboxOrchestrator orchestrator,
        ILogger<SandboxJupyterProxyMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SandboxJupyterPaths.TryParse(context.Request.Path.Value ?? string.Empty, out var sessionId, out var remaining))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var auth = await context.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme).ConfigureAwait(false);
        if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Jupyter proxy unauthorized for session {SessionId}", sessionId);
            if (context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect("/Admin/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
            return;
        }

        var target = await _orchestrator.TryGetJupyterProxyTargetAsync(sessionId, context.RequestAborted)
            .ConfigureAwait(false);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Sandbox Jupyter session not found or not interactive.").ConfigureAwait(false);
            return;
        }

        if ((remaining == "/" || string.IsNullOrEmpty(remaining))
            && HttpMethods.IsGet(context.Request.Method)
            && !context.WebSockets.IsWebSocketRequest)
        {
            context.Response.Redirect(SandboxJupyterPaths.GetLabEntryUrl(sessionId));
            return;
        }

        // Jupyter 以 base_url=/sandbox-jupyter/{id}/ 启动，后端路径需保留前缀
        var backendPath = SandboxJupyterPaths.GetBaseUrl(sessionId).TrimEnd('/') + remaining;

        _logger.LogDebug(
            "Jupyter proxy {Method} session={SessionId} path={Path} -> 127.0.0.1:{Port}{BackendPath}",
            context.Request.Method,
            target.SessionId,
            context.Request.Path,
            target.HostPort,
            backendPath);

        if (context.WebSockets.IsWebSocketRequest)
        {
            await ProxyWebSocketAsync(context, target, backendPath).ConfigureAwait(false);
            return;
        }

        await ProxyHttpAsync(context, target, backendPath).ConfigureAwait(false);
    }

    private async Task ProxyHttpAsync(HttpContext context, SandboxJupyterProxyTarget target, string backendPath)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var destination = new Uri($"http://127.0.0.1:{target.HostPort}{backendPath}{context.Request.QueryString}");

        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), destination);
        if (context.Request.ContentLength > 0
            || HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }

        CopyRequestHeaders(context.Request, request);
        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", "token " + target.AccessToken);

        using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted)
            .ConfigureAwait(false);

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task ProxyWebSocketAsync(HttpContext context, SandboxJupyterProxyTarget target, string backendPath)
    {
        var query = AppendTokenQuery(context.Request.QueryString.Value, target.AccessToken);
        var destination = new Uri($"ws://127.0.0.1:{target.HostPort}{backendPath}?{query}");

        using var serverSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        using var clientSocket = new ClientWebSocket();
        clientSocket.Options.SetRequestHeader("Authorization", "token " + target.AccessToken);

        try
        {
            await clientSocket.ConnectAsync(destination, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Jupyter WebSocket connect failed for session {SessionId}", target.SessionId);
            if (serverSocket.State == WebSocketState.Open)
            {
                await serverSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "upstream connect failed", CancellationToken.None)
                    .ConfigureAwait(false);
            }

            return;
        }

        var toUpstream = PumpAsync(serverSocket, clientSocket, context.RequestAborted);
        var toClient = PumpAsync(clientSocket, serverSocket, context.RequestAborted);
        await Task.WhenAny(toUpstream, toClient).ConfigureAwait(false);
    }

    private static async Task PumpAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 16];
        while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
        {
            var result = await source.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (destination.State == WebSocketState.Open)
                {
                    await destination.CloseAsync(
                            source.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                            source.CloseStatusDescription,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                break;
            }

            await destination.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static void CopyRequestHeaders(HttpRequest source, HttpRequestMessage destination)
    {
        foreach (var header in source.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)
                || header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!destination.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())
                && destination.Content != null)
            {
                destination.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static string AppendTokenQuery(string? query, string token)
    {
        var q = (query ?? string.Empty).TrimStart('?');
        if (q.Contains("token=", StringComparison.OrdinalIgnoreCase))
        {
            return q;
        }

        return string.IsNullOrEmpty(q) ? "token=" + Uri.EscapeDataString(token) : q + "&token=" + Uri.EscapeDataString(token);
    }
}
