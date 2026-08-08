/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeController.cs
    文件功能描述：DesktopBridge 能力探测、快照和 SSE 接口

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

    修改标识：Senparc - 20260729
    修改描述：v0.1.1-preview3 复用 NCF 标准 AdminOnly 策略名称

    修改标识：Senparc - 20260808
    修改描述：v0.4.0-preview4 新增管理员换票发起与兑换接口

----------------------------------------------------------------*/

using System.Text.Json;
using System.Security.Claims;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.Security;
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
    private readonly DesktopAdminAuthHandoffStore _adminAuthHandoffStore;
    private readonly IDesktopAdminAuthTokenIssuer? _adminAuthTokenIssuer;

    public DesktopBridgeController(
        DesktopActivityHub activityHub,
        DesktopAuthorizedSyncHub authorizedSyncHub,
        DesktopBridgeTokenValidator tokenValidator,
        DesktopAdminAuthHandoffStore? adminAuthHandoffStore = null,
        IEnumerable<IDesktopAdminAuthTokenIssuer>? adminAuthTokenIssuers = null)
    {
        _activityHub = activityHub;
        _authorizedSyncHub = authorizedSyncHub;
        _tokenValidator = tokenValidator;
        _adminAuthHandoffStore = adminAuthHandoffStore ?? new DesktopAdminAuthHandoffStore();
        var issuers = adminAuthTokenIssuers?.Take(2).ToArray() ?? [];
        _adminAuthTokenIssuer = issuers.Length == 1 ? issuers[0] : null;
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
            AuthorizedSyncEndpoint: "/api/Senparc.Xncf.DesktopBridge/authorized-sync/events",
            SupportsAdminAuthHandoff: _adminAuthTokenIssuer != null,
            AdminAuthHandoffRequestEndpoint: _adminAuthTokenIssuer == null
                ? null
                : "/api/Senparc.Xncf.DesktopBridge/admin-auth-handoff/requests",
            AdminAuthHandoffRedeemEndpoint: _adminAuthTokenIssuer == null
                ? null
                : "/api/Senparc.Xncf.DesktopBridge/admin-auth-handoff/redeem");
    }

    /// <summary>
    /// 创建绑定当前 DesktopBridge 会话的 PKCE 挑战。此接口不接受、读取或返回浏览器 Cookie。
    /// </summary>
    [HttpPost("admin-auth-handoff/requests")]
    [AllowAnonymous]
    [RequestSizeLimit(4096)]
    public ActionResult<DesktopAdminAuthHandoffCreateResponse> CreateAdminAuthHandoff(
        [FromBody] DesktopAdminAuthHandoffCreateRequest request)
    {
        SetNoStoreHeaders();
        if (_adminAuthTokenIssuer == null)
        {
            return NotFound();
        }

        var transportFailure = ValidateSecureTransport();
        if (transportFailure != null)
        {
            return transportFailure;
        }

        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        var desktopSessionToken = Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName].FirstOrDefault();
        try
        {
            return Ok(_adminAuthHandoffStore.Create(
                desktopSessionToken!,
                request.CodeChallenge,
                request.ReturnPath));
        }
        catch (DesktopAdminAuthHandoffRateLimitException ex)
        {
            Response.Headers.RetryAfter = "30";
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ProblemDetails
                {
                    Title = "桌面登录授权请求过于频繁",
                    Detail = ex.Message,
                    Status = StatusCodes.Status429TooManyRequests
                });
        }
        catch (DesktopAdminAuthHandoffException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "桌面登录授权请求无效",
                Detail = "请重新发起 WebView 管理员授权。",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// 使用仅存在于 GUI 内存中的 PKCE verifier 领取 JWT。批准后的挑战无论成功失败都只可消费一次。
    /// </summary>
    [HttpPost("admin-auth-handoff/redeem")]
    [AllowAnonymous]
    [RequestSizeLimit(4096)]
    public async Task<ActionResult<DesktopAdminAuthHandoffRedeemResponse>> RedeemAdminAuthHandoff(
        [FromBody] DesktopAdminAuthHandoffRedeemRequest request,
        CancellationToken cancellationToken = default)
    {
        SetNoStoreHeaders();
        if (_adminAuthTokenIssuer == null)
        {
            return NotFound();
        }

        var transportFailure = ValidateSecureTransport();
        if (transportFailure != null)
        {
            return transportFailure;
        }

        var authorizationFailure = AuthorizeDesktopSession();
        if (authorizationFailure != null)
        {
            return authorizationFailure;
        }

        var desktopSessionToken = Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName].FirstOrDefault();
        var handoff = _adminAuthHandoffStore.Redeem(
            request.RequestId,
            desktopSessionToken!,
            request.CodeVerifier);
        if (string.Equals(handoff.Status, "pending", StringComparison.Ordinal))
        {
            return Accepted(new DesktopAdminAuthHandoffRedeemResponse("pending"));
        }

        if (string.Equals(handoff.Status, "denied", StringComparison.Ordinal))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new DesktopAdminAuthHandoffRedeemResponse(
                    "denied",
                    Message: handoff.Message ?? "WebView 登录不能用于桌面授权。"));
        }

        if (!string.Equals(handoff.Status, "approved", StringComparison.Ordinal) ||
            handoff.AdminUserId is not { } adminUserId ||
            handoff.SourceAuthenticationExpiresUtc is not { } sourceExpiresUtc)
        {
            return BadRequest(new DesktopAdminAuthHandoffRedeemResponse(
                "invalid",
                Message: "一次性授权无效或已过期。"));
        }

        var token = await _adminAuthTokenIssuer
            .IssueAsync(adminUserId, sourceExpiresUtc, cancellationToken)
            .ConfigureAwait(false);
        if (!token.Succeeded || string.IsNullOrWhiteSpace(token.UserName) ||
            string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresUtc == null)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new DesktopAdminAuthHandoffRedeemResponse(
                    "denied",
                    Message: token.ErrorMessage ?? "管理员身份不能用于桌面授权。"));
        }

        return Ok(new DesktopAdminAuthHandoffRedeemResponse(
            "approved",
            token.UserName,
            token.AccessToken,
            token.ExpiresUtc));
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

    private ActionResult? ValidateSecureTransport()
    {
        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        if (Request.IsHttps || remoteAddress != null && IPAddress.IsLoopback(remoteAddress))
        {
            return null;
        }

        return StatusCode(
            StatusCodes.Status403Forbidden,
            new ProblemDetails
            {
                Title = "桌面登录授权要求安全传输",
                Detail = "远程换票必须使用 HTTPS；HTTP 仅允许 localhost 或本机 SSH 隧道。",
                Status = StatusCodes.Status403Forbidden
            });
    }

    private void SetNoStoreHeaders()
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
    }
}
