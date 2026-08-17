/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNcfPreviewProxyMiddleware.cs
    文件功能描述：将管理员请求代理到仅监听 loopback 的 NCF 预览容器

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

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
/// /sandbox-preview/{sessionId}/... routes only to a running NCF preview container bound on
/// 127.0.0.1. The browser's cookies and Authorization header are never forwarded upstream.
/// </summary>
public sealed class SandboxNcfPreviewProxyMiddleware
{
    public const string HttpClientName = "Senparc.Xncf.Sandbox.NcfPreviewProxy";

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailers", "Transfer-Encoding", "Upgrade", "Host", "Content-Length"
    };

    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SandboxOrchestrator _orchestrator;
    private readonly ILogger<SandboxNcfPreviewProxyMiddleware> _logger;

    public SandboxNcfPreviewProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        SandboxOrchestrator orchestrator,
        ILogger<SandboxNcfPreviewProxyMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SandboxNcfPreviewPaths.TryParse(context.Request.Path.Value ?? string.Empty, out var sessionId, out var remaining))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var auth = await context.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme).ConfigureAwait(false);
        if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect("/Admin/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));
            return;
        }

        var target = await _orchestrator.TryGetNcfPreviewProxyTargetAsync(sessionId, context.RequestAborted)
            .ConfigureAwait(false);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Sandbox NCF preview session not found or not running.").ConfigureAwait(false);
            return;
        }

        // The preview host opts into NCF_XNCF_PREVIEW_PATH_BASE. Retaining the prefix lets its
        // UsePathBase middleware generate correct root-relative links and callbacks.
        var backendPath = SandboxNcfPreviewPaths.GetBasePath(target.SessionId) + remaining;
        _logger.LogDebug(
            "NCF preview proxy {Method} session={SessionId} path={Path} -> 127.0.0.1:{Port}{BackendPath}",
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

    private async Task ProxyHttpAsync(HttpContext context, SandboxNcfPreviewProxyTarget target, string backendPath)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var destination = new Uri($"http://127.0.0.1:{target.HostPort}{backendPath}{context.Request.QueryString}");
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), destination);
        if (context.Request.ContentLength > 0 || HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrEmpty(context.Request.ContentType))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }
        CopyRequestHeaders(context.Request, request);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", SandboxNcfPreviewPaths.GetBasePath(target.SessionId));

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted)
            .ConfigureAwait(false);
        context.Response.StatusCode = (int)response.StatusCode;
        CopyResponseHeaders(response.Headers, context.Response.Headers);
        CopyResponseHeaders(response.Content.Headers, context.Response.Headers);
        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task ProxyWebSocketAsync(HttpContext context, SandboxNcfPreviewProxyTarget target, string backendPath)
    {
        var destination = new Uri($"ws://127.0.0.1:{target.HostPort}{backendPath}{context.Request.QueryString}");
        using var serverSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        using var clientSocket = new ClientWebSocket();
        clientSocket.Options.SetRequestHeader("X-Forwarded-Prefix", SandboxNcfPreviewPaths.GetBasePath(target.SessionId));
        try
        {
            await clientSocket.ConnectAsync(destination, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NCF preview WebSocket connection failed for session {SessionId}", target.SessionId);
            if (serverSocket.State == WebSocketState.Open)
            {
                await serverSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "upstream connect failed", CancellationToken.None)
                    .ConfigureAwait(false);
            }
            return;
        }

        await Task.WhenAny(
                PumpAsync(serverSocket, clientSocket, context.RequestAborted),
                PumpAsync(clientSocket, serverSocket, context.RequestAborted))
            .ConfigureAwait(false);
    }

    private static async Task PumpAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
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
                return;
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
            if (!destination.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && destination.Content != null)
            {
                destination.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private static void CopyResponseHeaders(HttpHeaders source, IHeaderDictionary destination)
    {
        foreach (var header in source)
        {
            if (!HopByHopHeaders.Contains(header.Key)
                && !header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                destination[header.Key] = header.Value.ToArray();
            }
        }
    }
}
