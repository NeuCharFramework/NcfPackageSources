/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowChatController.cs
    File description: Chat trigger page and anonymous HTTP entry points

    Created by: Senparc - 20260909
    Creation description: v0.4.0 add the Chat trigger

    Modified by: Senparc - 20260915
    Change description: v0.4.0 improve Chat message persistence and recovery

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
using Senparc.Xncf.NeuCharWorkflow;

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

    /// <summary>
    /// Initializes the anonymous workflow-chat HTTP controller.
    /// </summary>
    /// <param name="workflowAppService">The service used for owner/guest chat operations.</param>
    public NeuCharWorkflowChatController(
        NeuCharWorkflowAppService workflowAppService)
    {
        _workflowAppService = workflowAppService;
    }

    /// <summary>
    /// Open the chat page. The page carries no session data; the client loads
    /// it through the JSON endpoints below.
    /// </summary>
    /// <summary>
    /// Renders the localized static chat page shell for one workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier embedded in the page.</param>
    /// <returns>An HTML response containing the chat shell and translations.</returns>
    [HttpGet("{workflowId:int}/page")]
    public IActionResult Page(int workflowId)
    {
        EnsureGuestCookie();
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        var translations = new
        {
            title = NeuCharWorkflowResource.Get("Chat.Title", "NeuChar Workflow Chat"),
            loading = NeuCharWorkflowResource.Get("Chat.Loading", "Loading…"),
            newConversation = NeuCharWorkflowResource.Get("Chat.NewConversation", "New conversation"),
            inputPlaceholder = NeuCharWorkflowResource.Get("Chat.InputPlaceholder", "Type a message and press Enter to send"),
            send = NeuCharWorkflowResource.Get("Chat.Send", "Send"),
            enterHint = NeuCharWorkflowResource.Get("Chat.EnterHint", "Enter to send · Shift + Enter for a new line · Your message starts a Workflow run"),
            processing = NeuCharWorkflowResource.Get("Chat.Processing", "Processing…"),
            previousProcessing = NeuCharWorkflowResource.Get("Chat.PreviousProcessing", "The previous message is still processing…"),
            starting = NeuCharWorkflowResource.Get("Chat.Starting", "Starting Workflow…"),
            runFailed = NeuCharWorkflowResource.Get("Chat.RunFailed", "Workflow execution failed."),
            runCompleted = NeuCharWorkflowResource.Get("Chat.RunCompleted", "Workflow completed."),
            clearConfirm = NeuCharWorkflowResource.Get("Chat.ClearConfirm", "Clear the current conversation? Any running Workflow is not affected."),
            resetFailed = NeuCharWorkflowResource.Get("Chat.ResetFailed", "Reset failed."),
            sessionLoadFailed = NeuCharWorkflowResource.Get("Chat.SessionLoadFailed", "Unable to load the chat session."),
            sessionUnavailable = NeuCharWorkflowResource.Get("Chat.SessionUnavailable", "Unable to load the session"),
            guestMode = NeuCharWorkflowResource.Get("Chat.GuestMode", "Guest mode"),
            signedIn = NeuCharWorkflowResource.Get("Chat.SignedIn", "Signed in"),
            trigger = NeuCharWorkflowResource.Get("Chat.Trigger", "Chat trigger"),
            user = NeuCharWorkflowResource.Get("Chat.User", "Me")
        };
        var html = NeuCharWorkflowChatPage.Template
            .Replace("__WORKFLOW_ID__", workflowId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("__CHAT_LANG__", culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh-CN" : culture.Name)
            .Replace("__CHAT_I18N__", JsonSerializer.Serialize(translations, JsonOptions));
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("{workflowId:int}/bootstrap")]
    [RequestSizeLimit(MaxRequestBytes)]
    /// <summary>
    /// Returns bootstrap metadata and persisted messages for a workflow chat participant.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">Token used to cancel loading.</param>
    /// <returns>An HTTP result containing chat bootstrap state.</returns>
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
    /// <summary>
    /// Accepts one participant message and starts a workflow chat run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="request">The message payload.</param>
    /// <param name="cancellationToken">Token used to cancel submission.</param>
    /// <returns>An HTTP 202 result when a run starts, or a mapped error result.</returns>
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
    /// <summary>
    /// Returns participant-scoped incremental status for a chat workflow run.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="runId">The run identifier.</param>
    /// <param name="afterSequence">The last sequence already consumed.</param>
    /// <param name="cancellationToken">Token used to cancel status loading.</param>
    /// <returns>An HTTP result containing run state and latest output.</returns>
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
    /// <summary>
    /// Clears the participant's persisted chat session for a workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">Token used to cancel reset.</param>
    /// <returns>An HTTP result indicating whether reset completed.</returns>
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

    /// <summary>
    /// Validates the fixed-length URL-safe guest token format.
    /// </summary>
    /// <param name="value">The cookie value to validate.</param>
    /// <returns><see langword="true"/> when the value contains exactly 43 URL-safe characters.</returns>
    private static bool IsValidGuestToken([NotNullWhen(true)] string? value) =>
        value is { Length: 43 }
        && value.All(z => (z >= 'A' && z <= 'Z') || (z >= 'a' && z <= 'z') || (z >= '0' && z <= '9') || z is '-' or '_');

    /// <summary>
    /// Generates a URL-safe guest token from 32 cryptographically random bytes.
    /// </summary>
    /// <returns>The Base64Url-encoded guest token.</returns>
    private static string GenerateGuestToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes.ToArray());
    }

    /// <summary>
    /// Carries one participant message submitted to the workflow chat endpoint.
    /// </summary>
    public sealed record ChatMessageRequest(string? Message);
}
