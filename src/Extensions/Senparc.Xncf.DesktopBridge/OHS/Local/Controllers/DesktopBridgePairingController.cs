/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgePairingController.cs
    文件功能描述：DesktopBridge 匿名设备配对申请和凭据领取接口

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.OHS.Local.Controllers;

[ApiController]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/Senparc.Xncf.DesktopBridge/pairing")]
public sealed class DesktopBridgePairingController : ControllerBase
{
    private readonly DesktopBridgeCredentialStore _credentialStore;

    public DesktopBridgePairingController(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    [HttpPost("requests")]
    [RequestSizeLimit(4096)]
    public ActionResult<DesktopBridgePairingCreateResponse> CreateRequest(
        [FromBody] DesktopBridgePairingCreateRequest request)
    {
        var transportFailure = ValidateTransport();
        if (transportFailure != null)
        {
            return transportFailure;
        }

        try
        {
            var pairing = _credentialStore.CreatePairingRequest(
                request.ClientName,
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(pairing);
        }
        catch (DesktopBridgePairingRateLimitException ex)
        {
            Response.Headers.RetryAfter = "30";
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new ProblemDetails
                {
                    Title = DesktopBridgeResource.Get("Pairing.RateLimit.Title"),
                    Detail = ex.Message,
                    Status = StatusCodes.Status429TooManyRequests
                });
        }
    }

    [HttpPost("poll")]
    [RequestSizeLimit(4096)]
    public ActionResult<DesktopBridgePairingPollResponse> Poll(
        [FromBody] DesktopBridgePairingPollRequest request)
    {
        var transportFailure = ValidateTransport();
        if (transportFailure != null)
        {
            return transportFailure;
        }

        var result = _credentialStore.Poll(request.RequestId, request.PollSecret);
        if (string.Equals(result.Status, "invalid", StringComparison.Ordinal))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = DesktopBridgeResource.Get("Pairing.Invalid.Title"),
                Detail = DesktopBridgeResource.Get("Pairing.Invalid.Detail"),
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(new DesktopBridgePairingPollResponse(
            result.Status,
            result.SessionToken,
            result.SessionExpiresAt,
            result.Message));
    }

    private ActionResult? ValidateTransport()
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
                Title = DesktopBridgeResource.Get("Pairing.Transport.Title"),
                Detail = DesktopBridgeResource.Get("Pairing.Transport.Detail"),
                Status = StatusCodes.Status403Forbidden
            });
    }
}

