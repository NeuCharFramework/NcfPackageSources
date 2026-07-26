/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeController.cs
    文件功能描述：DesktopBridge 能力探测、快照和 SSE 接口

    创建标识：Senparc - 20260725
----------------------------------------------------------------*/

using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Xncf.DesktopBridge")]
public sealed class DesktopBridgeController : ControllerBase
{
    public const int CurrentProtocolVersion = 1;
    public const string BridgeVersion = "0.1.0-preview2";
    private const string AdminOnlyPolicy = "AdminOnly";
    private const string BackendJwtScheme = "Bearer_Backend";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DesktopActivityHub _activityHub;
    private readonly DesktopAuthorizedSyncHub _authorizedSyncHub;
    private readonly DesktopBridgeTokenValidator _tokenValidator;

    public DesktopBridgeController(
        DesktopActivityHub activityHub,
        DesktopAuthorizedSyncHub authorizedSyncHub,
        DesktopBridgeTokenValidator tokenValidator)
    {
        _activityHub = activityHub;
        _authorizedSyncHub = authorizedSyncHub;
        _tokenValidator = tokenValidator;
    }

    [HttpGet("capabilities")]
    public ActionResult<DesktopBridgeCapabilities> GetCapabilities()
    {
        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return new DesktopBridgeCapabilities(
            CurrentProtocolVersion,
            BridgeVersion,
            SupportsSse: true,
            SupportsSnapshot: true,
            EventEndpoint: "/api/Senparc.Xncf.DesktopBridge/events",
            SnapshotEndpoint: "/api/Senparc.Xncf.DesktopBridge/activities",
            SupportsAuthorizedSync: true,
            AuthorizedSyncEndpoint: "/api/Senparc.Xncf.DesktopBridge/authorized-sync/events");
    }

    [HttpGet("activities")]
    public ActionResult<IReadOnlyList<DesktopActivityMessage>> GetActivities()
    {
        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        return Ok(_activityHub.GetActiveSnapshot());
    }

    [HttpGet("events")]
    public async Task GetEvents(bool replayBuffered = true, CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            await authorizationFailure.ExecuteResultAsync(ControllerContext).ConfigureAwait(false);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await Response.WriteAsync(": connected\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var activity in _activityHub.Subscribe(replayBuffered, cancellationToken).ConfigureAwait(false))
        {
            var payload = JsonSerializer.Serialize(activity, JsonOptions);
            await Response.WriteAsync($"id: {activity.Sequence}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"event: activity\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 输出按当前管理员隔离的资源变更通知。正文仍需由客户端使用同一 JWT 从业务 API 读取。
    /// </summary>
    [HttpGet("authorized-sync/events")]
    [Authorize(AuthenticationSchemes = BackendJwtScheme, Policy = AdminOnlyPolicy)]
    public async Task GetAuthorizedSyncEvents(
        bool replayBuffered = true,
        CancellationToken cancellationToken = default)
    {
        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            await authorizationFailure.ExecuteResultAsync(ControllerContext).ConfigureAwait(false);
            return;
        }

        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await Response.WriteAsync(": connected\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var message in _authorizedSyncHub
                           .Subscribe(ownerId, AdminOnlyPolicy, replayBuffered, cancellationToken)
                           .ConfigureAwait(false))
        {
            var payload = JsonSerializer.Serialize(message, JsonOptions);
            await Response.WriteAsync($"id: {message.Sequence}\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync("event: authorized-sync\n", cancellationToken).ConfigureAwait(false);
            await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
            await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ActionResult? AuthorizeDesktopSession()
    {
        if (!_tokenValidator.IsConfigured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "DesktopBridge is inactive",
                    Detail = "Start NCF from NcfDesktopApp to enable a protected desktop session.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }

        var suppliedToken = Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName].FirstOrDefault();
        return _tokenValidator.IsAuthorized(suppliedToken) ? null : Unauthorized();
    }
}
