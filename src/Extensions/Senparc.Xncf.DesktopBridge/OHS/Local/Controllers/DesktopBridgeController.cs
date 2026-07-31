/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeController.cs
    文件功能描述：DesktopBridge 能力探测、快照和 SSE 接口

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

    修改标识：Senparc - 20260729
    修改描述：v0.1.1-preview3 复用 NCF 标准 AdminOnly 策略名称

----------------------------------------------------------------*/

using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.OHS.Local.Controllers;

[ApiController]
[Route("api/Senparc.Xncf.DesktopBridge")]
public sealed class DesktopBridgeController : ControllerBase
{
    public const int CurrentProtocolVersion = 1;
    public const string BridgeVersion = "0.2.1-preview2";
    private const string AdminOnlyPolicy = NcfAuthorizationPolicyNames.AdminOnly;
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
        var authorizationFailure = AuthorizeDesktopSession(out var sessionRevoked);
        if (authorizationFailure != null)
        {
            await authorizationFailure.ExecuteResultAsync(ControllerContext).ConfigureAwait(false);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        using var streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sessionRevoked);
        try
        {
            await Response.WriteAsync(": connected\n\n", streamCancellation.Token).ConfigureAwait(false);
            await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);

            await foreach (var activity in _activityHub
                               .Subscribe(replayBuffered, streamCancellation.Token)
                               .ConfigureAwait(false))
            {
                if (!HasAuthorizedDesktopSession())
                {
                    return;
                }

                var payload = JsonSerializer.Serialize(activity, JsonOptions);
                await Response.WriteAsync($"id: {activity.Sequence}\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.WriteAsync("event: activity\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.WriteAsync($"data: {payload}\n\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (streamCancellation.IsCancellationRequested)
        {
            // 浏览器断开或管理员撤销会话时立即结束现有 SSE，不记录为服务端错误。
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
        var authorizationFailure = AuthorizeDesktopSession(out var sessionRevoked);
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

        using var streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sessionRevoked);
        try
        {
            await Response.WriteAsync(": connected\n\n", streamCancellation.Token).ConfigureAwait(false);
            await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);

            await foreach (var message in _authorizedSyncHub
                               .Subscribe(ownerId, AdminOnlyPolicy, replayBuffered, streamCancellation.Token)
                               .ConfigureAwait(false))
            {
                if (!HasAuthorizedDesktopSession())
                {
                    return;
                }

                var payload = JsonSerializer.Serialize(message, JsonOptions);
                await Response.WriteAsync($"id: {message.Sequence}\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.WriteAsync("event: authorized-sync\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.WriteAsync($"data: {payload}\n\n", streamCancellation.Token).ConfigureAwait(false);
                await Response.Body.FlushAsync(streamCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (streamCancellation.IsCancellationRequested)
        {
            // 会话撤销同时终止 Admin Chat 同步流，不需要额外控制连接。
        }
    }

    private ActionResult? AuthorizeDesktopSession()
    {
        return AuthorizeDesktopSession(out _);
    }

    private ActionResult? AuthorizeDesktopSession(out CancellationToken sessionRevoked)
    {
        sessionRevoked = CancellationToken.None;
        var suppliedToken = Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(suppliedToken) && !_tokenValidator.IsConfigured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "DesktopBridge is inactive",
                    Detail = "Provide NCF_DESKTOP_BRIDGE_TOKEN or approve a DesktopBridge pairing request in Admin.",
                    Status = StatusCodes.Status503ServiceUnavailable
                });
        }

        return _tokenValidator.TryAuthorize(suppliedToken, out sessionRevoked) ? null : Unauthorized();
    }

    private bool HasAuthorizedDesktopSession()
    {
        var suppliedToken = Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName].FirstOrDefault();
        return _tokenValidator.IsAuthorized(suppliedToken);
    }
}
