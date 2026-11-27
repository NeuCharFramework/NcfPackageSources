/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxJupyterProxyMiddleware.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

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
/// Proxies authenticated Jupyter requests from the public sandbox route to the
/// loopback port owned by an interactive sandbox session.
/// </summary>
public sealed class SandboxJupyterProxyMiddleware
{
    /// <summary>Gets the named HTTP client used for Jupyter proxy requests.</summary>
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

    /// <summary>
    /// Initializes the Jupyter proxy middleware.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="httpClientFactory">The factory for outbound proxy clients.</param>
    /// <param name="orchestrator">The sandbox orchestrator resolving session targets.</param>
    /// <param name="logger">The proxy diagnostic logger.</param>
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

    /// <summary>
    /// Authenticates and proxies one Jupyter HTTP or WebSocket request to the sandbox session.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
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

        // Jupyter starts with base_url=/sandbox-jupyter/{id}/; the backend path must retain the prefix
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

    /// <summary>
    /// Proxies an HTTP request to the authenticated Jupyter backend while filtering hop-by-hop headers.
    /// </summary>
    /// <param name="context">The incoming HTTP context.</param>
    /// <param name="target">The resolved sandbox target and access token.</param>
    /// <param name="backendPath">The backend path including the Jupyter base path.</param>
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

    /// <summary>
    /// Bridges a client WebSocket to the Jupyter backend WebSocket.
    /// </summary>
    /// <param name="context">The incoming WebSocket context.</param>
    /// <param name="target">The resolved sandbox target and access token.</param>
    /// <param name="backendPath">The backend WebSocket path.</param>
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

    /// <summary>
    /// Copies WebSocket frames until either side closes or cancellation is requested.
    /// </summary>
    /// <param name="source">The source socket.</param>
    /// <param name="destination">The destination socket.</param>
    /// <param name="cancellationToken">Token used to stop frame pumping.</param>
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

    /// <summary>
    /// Copies safe request headers while excluding hop-by-hop, authorization, and cookie headers.
    /// </summary>
    /// <param name="source">The incoming request.</param>
    /// <param name="destination">The upstream request.</param>
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

    /// <summary>
    /// Adds the Jupyter token query parameter when one is not already present.
    /// </summary>
    /// <param name="query">The existing query string.</param>
    /// <param name="token">The access token.</param>
    /// <returns>The query string containing a token.</returns>
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
