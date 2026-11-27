/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopBridgePairingController.cs
    File description: Related implementation.

    Created by: Senparc - 20260801

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.12.2 Improve source documentation, headers, and module metadata consistency.

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

    /// <summary>
    /// Initializes the anonymous desktop pairing controller.
    /// </summary>
    /// <param name="credentialStore">The pairing/session credential store.</param>
    public DesktopBridgePairingController(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    [HttpPost("requests")]
    [RequestSizeLimit(4096)]
    /// <summary>
    /// Creates a loopback/HTTPS desktop pairing request.
    /// </summary>
    /// <param name="request">The desktop client name and pairing metadata.</param>
    /// <returns>The pairing data or a mapped transport/rate-limit error.</returns>
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
                    Title = "配对请求过于频繁",
                    Detail = ex.Message,
                    Status = StatusCodes.Status429TooManyRequests
                });
        }
    }

    [HttpPost("poll")]
    [RequestSizeLimit(4096)]
    /// <summary>
    /// Polls a pairing request using its one-time polling secret.
    /// </summary>
    /// <param name="request">The pairing request identifier and poll secret.</param>
    /// <returns>The current pairing status or an unauthorized response.</returns>
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
                Title = "配对凭据无效",
                Detail = "配对请求不存在或领取凭据不正确。",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(new DesktopBridgePairingPollResponse(
            result.Status,
            result.SessionToken,
            result.SessionExpiresAt,
            result.Message));
    }

    /// <summary>
    /// Rejects remote HTTP pairing while allowing HTTPS and loopback transport.
    /// </summary>
    /// <returns>An HTTP error result when transport is unsafe; otherwise <see langword="null"/>.</returns>
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
                Title = "DesktopBridge 配对要求安全传输",
                Detail = "远程配对必须使用 HTTPS；HTTP 仅允许 localhost 或本机 SSH 隧道。",
                Status = StatusCodes.Status403Forbidden
            });
    }
}

