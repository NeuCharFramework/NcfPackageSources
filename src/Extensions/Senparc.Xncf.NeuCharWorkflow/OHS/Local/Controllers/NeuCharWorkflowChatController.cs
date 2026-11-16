/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: NeuCharWorkflowChatController.cs
    File description: Chat trigger page and anonymous HTTP entry points


    Created by: Senparc - 20260909
    Creation description: v0.4.0 add the Chat trigger

    Modified by: Senparc - 20260915
    Change description: v0.4.0 improve Chat message persistence and recovery

----------------------------------------------------------------*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;

namespace Senparc.Xncf.NeuCharWorkflow.OHS.Local.Controllers;

/// <summary>
/// Entry point for the NeuChar Workflow Chat trigger. The page can be opened by
/// authenticated users or guests; identity comes from claims or an HttpOnly
/// session cookie, while the workflow remains coordinated on the server.
/// </summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/chat")]
public sealed class NeuCharWorkflowChatController : ControllerBase
{
    private const string GuestCookieName = "nxcf_wf_chat_guest";
    private const int MaxRequestBytes = 65_536;
    private static readonly TimeSpan GuestCookieDuration = TimeSpan.FromDays(7);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly NeuCharWorkflowAppService _workflowAppService;

    public NeuCharWorkflowChatController(
        NeuCharWorkflowAppService workflowAppService)
    {
        _workflowAppService = workflowAppService;
    }

    /// <summary>
    /// Open the chat page. The page carries no session data; the client loads
    /// it through the JSON endpoints below.
    /// </summary>
    [HttpGet("{workflowId:int}/page")]
    public IActionResult Page(int workflowId)
    {
        EnsureGuestCookie();
        var html = NeuCharWorkflowChatPage.Template
            .Replace("__WORKFLOW_ID__", workflowId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("{workflowId:int}/bootstrap")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Bootstrap(int workflowId, CancellationToken cancellationToken)
    {
        EnsureGuestCookie();
        var participantKey = ResolveParticipantKey();
        var result = await _workflowAppService.GetChatBootstrapAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode != StatusCodes.Status200OK)
        {
            return StatusCode(result.StatusCode, new { success = false, errorMessage = result.ErrorMessage });
        }
        return Ok(new
        {
            success = true,
            workflowId = result.WorkflowId,
            title = result.Title,
            greeting = result.Greeting,
            isGuest = result.IsGuest,
            hasPendingRun = result.HasPendingRun,
            pendingRunId = result.PendingRunId,
            messages = result.Messages
        });
    }

    [HttpPost("{workflowId:int}/messages")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> SendMessage(
        int workflowId,
        [FromBody] ChatMessageRequest? request,
        CancellationToken cancellationToken)
    {
        EnsureGuestCookie();
        var participantKey = ResolveParticipantKey();
        var result = await _workflowAppService.SendChatMessageAsync(
            workflowId,
            participantKey,
            request?.Message,
            cancellationToken).ConfigureAwait(false);
        return StatusCode(result.StatusCode, result.StatusCode == StatusCodes.Status202Accepted
            ? new { success = true, workflowId = result.WorkflowId, runId = result.RunId }
            : new { success = false, errorMessage = result.ErrorMessage });
    }

    [HttpGet("{workflowId:int}/runs/{runId:guid}")]
    public async Task<IActionResult> RunStatus(
        int workflowId,
        Guid runId,
        long afterSequence,
        CancellationToken cancellationToken)
    {
        EnsureGuestCookie();
        var participantKey = ResolveParticipantKey();
        var result = await _workflowAppService.GetChatRunStatusAsync(
            workflowId,
            participantKey,
            runId,
            Math.Max(0, afterSequence),
            cancellationToken).ConfigureAwait(false);
        if (result.StatusCode != StatusCodes.Status200OK)
        {
            return StatusCode(result.StatusCode, new { success = false, errorMessage = result.ErrorMessage });
        }
        return Ok(new
        {
            success = true,
            running = result.Running,
            finalOutput = result.FinalOutput,
            runError = result.RunError,
            lastNodeMessage = result.LastNodeMessage,
            lastSequence = result.LastSequence
        });
    }

    [HttpPost("{workflowId:int}/reset")]
    public async Task<IActionResult> Reset(int workflowId, CancellationToken cancellationToken)
    {
        EnsureGuestCookie();
        var participantKey = ResolveParticipantKey();
        var result = await _workflowAppService.ResetChatSessionAsync(workflowId, participantKey, cancellationToken).ConfigureAwait(false);
        return StatusCode(result.StatusCode, result.StatusCode == StatusCodes.Status200OK
            ? new { success = true }
            : new { success = false, errorMessage = result.ErrorMessage });
    }

    /// <summary>
    /// Authenticated users use claims, while guests use an HttpOnly cookie.
    /// The two identities are never mixed within one workflow.
    /// </summary>
    private string ResolveParticipantKey()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var identity = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity.Name;
            return string.IsNullOrWhiteSpace(identity)
                ? "user:anonymous"
                : "user:" + identity.Trim();
        }
        return "guest:" + EnsureGuestCookie();
    }

    /// <summary>
    /// Read or create the guest session token. It is a Base64Url encoding of
    /// 32 random bytes and exists only in the cookie.
    /// </summary>
    private string EnsureGuestCookie()
    {
        var existing = Request.Cookies[GuestCookieName];
        if (IsValidGuestToken(existing))
        {
            return existing;
        }
        var token = GenerateGuestToken();
        Response.Cookies.Append(GuestCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Path = "/",
            Expires = DateTimeOffset.UtcNow + GuestCookieDuration
        });
        return token;
    }

    private static bool IsValidGuestToken([NotNullWhen(true)] string? value) =>
        value is { Length: 43 }
        && value.All(z => (z >= 'A' && z <= 'Z') || (z >= 'a' && z <= 'z') || (z >= '0' && z <= '9') || z is '-' or '_');

    private static string GenerateGuestToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes.ToArray());
    }

    public sealed record ChatMessageRequest(string? Message);
}
