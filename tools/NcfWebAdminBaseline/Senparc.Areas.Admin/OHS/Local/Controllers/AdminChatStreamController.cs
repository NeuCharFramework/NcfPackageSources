/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatStreamController.cs
    File description: Related implementation.

    Created by: Senparc - 20260726

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 implement AdminChat memory storage and management system

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

    Modified by: Senparc - 20261107
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Security.Claims;
using System.Text.Json;
using Senparc.Ncf.XncfBase.ChatTools;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.OHS.Local.Events;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Areas.Admin.OHS.Local.Controllers;

/// <summary>
///Legacy comment translated to English.
/// </summary>
[ApiController]
[Route("api/Senparc.Areas.Admin/AdminChatStream")]
[AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
public sealed class AdminChatStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AdminChatSessionService _sessionService;
    private readonly AdminChatMessageService _messageService;
    private readonly AdminChatAiService _chatAiService;
    private readonly AdminChatRunRegistry _runs;
    private readonly IStringLocalizer<AdminResource> _localizer;
    private readonly ILogger<AdminChatStreamController> _logger;
    private readonly IEventBus? _eventBus;
    private int _disconnectLogged;

    /// <summary>
    /// Initializes the authenticated AdminChat SSE controller and its session,
    /// persistence, AI, run-registry, localization, and event-bus dependencies.
    /// </summary>
    /// <param name="sessionService">The owner-scoped chat session service.</param>
    /// <param name="messageService">The chat message and artifact service.</param>
    /// <param name="chatAiService">The AI generation service.</param>
    /// <param name="runs">The in-memory active-run registry.</param>
    /// <param name="serviceProvider">The provider used for optional event-bus resolution.</param>
    /// <param name="localizer">The Admin resource localizer.</param>
    /// <param name="logger">The stream controller logger.</param>
    public AdminChatStreamController(
        AdminChatSessionService sessionService,
        AdminChatMessageService messageService,
        AdminChatAiService chatAiService,
        AdminChatRunRegistry runs,
        IServiceProvider serviceProvider,
        IStringLocalizer<AdminResource> localizer,
        ILogger<AdminChatStreamController> logger)
    {
        _sessionService = sessionService;
        _messageService = messageService;
        _chatAiService = chatAiService;
        _runs = runs;
        _localizer = localizer;
        _logger = logger;
        _eventBus = serviceProvider.GetService(typeof(IEventBus)) as IEventBus;
    }

    /// <summary>
    /// Validates owner access, persists the user message, streams live AI events,
    /// persists the assistant response, and emits a terminal SSE event.
    /// </summary>
    /// <param name="request">The session, content, mode, model, and attachment request.</param>
    /// <param name="cancellationToken">Token used to cancel request processing.</param>
    /// <returns>A task completing after the SSE stream is closed or cancelled.</returns>
    [HttpPost("send")]
    public async Task SendAsync(
        [FromBody] ChatMessageInputDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.SessionId <= 0
            || string.IsNullOrWhiteSpace(request.Content) && request.Attachments?.Count is not > 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = _localizer["AdminChat.StreamInputRequired"].Value }, cancellationToken);
            return;
        }

        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
        if (session == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = _localizer["AdminChat.StreamSessionForbidden"].Value }, cancellationToken);
            return;
        }

        var run = _runs.TryBegin(request.SessionId, userId);
        if (run == null)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = "该会话已有任务在执行。" }, CancellationToken.None);
            return;
        }

        var clientConnected = true;
        try
        {
        IReadOnlyList<AdminChatAttachment> attachments = Array.Empty<AdminChatAttachment>();
        var content = string.IsNullOrWhiteSpace(request.Content)
            ? AdminChatAttachmentService.AttachmentOnlyMessage
            : request.PromptReferences?.Count > 0 ? request.Content : request.Content.Trim();
        AdminChatMessage userMessage;
        try
        {
            if (request.Attachments?.Count > 0)
            {
                await HttpContext.RequestServices.GetRequiredService<AdminChatWorkspaceAssetService>()
                    .GetSessionAsync(request.SessionId, User, CancellationToken.None);
                attachments = await HttpContext.RequestServices.GetRequiredService<AdminChatAttachmentService>()
                    .ValidateAsync(session, request.Attachments, CancellationToken.None);
            }
            userMessage = await _messageService.AddMessageAsync(
                request.SessionId, ChatMessageRoleType.User, content,
                attachments: attachments, promptReferences: request.PromptReferences);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or UnauthorizedAccessException or IOException or NotSupportedException or System.Xml.XmlException
            or Senparc.Ncf.Core.Exceptions.NcfExceptionBase)
        {
            Response.StatusCode = error is UnauthorizedAccessException ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { success = false, errorMessage = error.Message }, CancellationToken.None);
            return;
        }
        await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

        try
        {
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.ContentType = "text/event-stream";
            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            clientConnected = await EmitAsync(run, "user-message", AdminChatMessageDto.CreateFromEntity(userMessage), clientConnected);
        }
        catch (Exception) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            clientConnected = false;
            NoteDisconnect(run);
        }

        var liveEvents = Channel.CreateUnbounded<AdminChatLiveEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var requiresHarness = request.Mode != AdminChatMode.Simple
            || ChatToolRegistry.Match(content).Count > 0
            || !string.IsNullOrWhiteSpace(session.WorkspaceId);
        var generationTask = requiresHarness
            ? GenerateHarnessResponseAsync(
                request.SessionId,
                userId,
                content,
                request.AiModelId,
                liveEvents.Writer,
                run.Token,
                attachments,
                request.Mode == AdminChatMode.Execute ? "execute" : "plan",
                request.HarnessOptions)
            : GenerateResponseAsync(
            request.SessionId,
            userId,
            content,
            request.AiModelId,
            liveEvents.Writer,
            run.Token,
            attachments,
            request.MaxOutputTokens);

        try
        {
            await foreach (var liveEvent in liveEvents.Reader.ReadAllAsync(run.Token).ConfigureAwait(false))
                clientConnected = await EmitLiveAsync(run, liveEvent, clientConnected).ConfigureAwait(false);

            var result = await generationTask.ConfigureAwait(false);
            var workspaceArtifacts = await _messageService
                .CollectWorkspaceArtifactsAsync(request.SessionId, result.Harness)
                .ConfigureAwait(false);
            var assistantMessage = await _messageService.AddMessageAsync(
                request.SessionId,
                ChatMessageRoleType.Assistant,
                result.Response,
                result.ModelIdentifier,
                result.Harness?.TrajectoryId,
                result.Harness?.TrajectorySequence,
                workspaceArtifacts);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new AdminChatSyncEvent(userId, request.SessionId, "messages-changed"));
            }
            clientConnected = await EmitAsync(run, "assistant-message", AdminChatMessageDto.CreateFromEntity(assistantMessage), clientConnected);
            await EmitAsync(run, "done", new
            {
                sessionId = request.SessionId,
                memoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                memoryUpdateResult = _chatAiService.MemoryUpdateResult,
                memoryUsage = _chatAiService.MemoryUsage,
                mode = request.Mode,
                trajectoryId = result.Harness?.TrajectoryId ?? 0,
                trajectorySequence = result.Harness?.TrajectorySequence ?? 0,
                trajectoryStatus = result.Harness?.Status,
                pendingApprovals = result.Harness?.PendingApprovals
            }, clientConnected);
        }
        catch (OperationCanceledException) when (run.Token.IsCancellationRequested)
        {
            await ObserveGenerationAsync(generationTask).ConfigureAwait(false);
            _logger.LogInformation("AdminChat 任务已按用户请求停止。SessionId={SessionId}", request.SessionId);
            await EmitAsync(run, "cancelled", new { message = "本次请求已终止" }, clientConnected);
        }
        catch (Exception ex)
        {
            await ObserveGenerationAsync(generationTask).ConfigureAwait(false);
            _logger.LogError(ex, "AdminChat 任务执行失败。SessionId={SessionId}", request.SessionId);
            await EmitAsync(run, "error", new
            {
                message = string.IsNullOrWhiteSpace(ex.Message) ? _localizer["AdminChat.AgentReplyFailed"].Value : ex.Message
            }, clientConnected);
        }
        }
        finally
        {
            _runs.Finish(run);
        }
    }

    /// <summary>
    /// Returns owner-scoped active/just-finished run status and accumulated text.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <returns>An HTTP 200 status object, 401, or 404.</returns>
    [HttpGet("status")]
    public async Task<IActionResult> Status(int sessionId)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0) return Unauthorized();
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null) return NotFound();
        var status = _runs.GetStatus(sessionId, userId);
        return Ok(new
        {
            running = status.Running,
            justFinished = status.JustFinished,
            text = status.Text,
            thinking = status.Thinking,
            status = status.Status
        });
    }

    /// <summary>
    /// Requests cancellation of the owner-scoped active run for a session.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <returns>The cancellation request outcome.</returns>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(int sessionId)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0) return Unauthorized();
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null) return NotFound();
        var cancelled = _runs.Cancel(sessionId, userId);
        _logger.LogInformation("AdminChat 收到停止请求。SessionId={SessionId}, Cancelled={Cancelled}", sessionId, cancelled);
        return Ok(new { cancelled });
    }

    /// <summary>
    /// Attaches to an active owner-scoped run, sends an initial snapshot, and
    /// streams subsequent SSE frames until completion or observer cancellation.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the observer connection.</param>
    [HttpGet("attach")]
    public async Task Attach(int sessionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentAdminUserInfoId();
        if (userId <= 0)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var run = _runs.Find(sessionId, userId);
        if (run == null)
        {
            Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        using var listener = _runs.Listen(run);
        try
        {
            await WriteRawEventAsync("resume", listener.SnapshotJson, cancellationToken).ConfigureAwait(false);
            await foreach (var frame in listener.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var split = frame.IndexOf('\n');
                await WriteRawEventAsync(frame[..split], frame[(split + 1)..], cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("AdminChat 观察者已断开，任务继续执行。SessionId={SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Converts one internal generation event into the corresponding SSE event,
    /// updates the active-run snapshot, and preserves the connection state when
    /// an event kind is not intended for the browser.
    /// </summary>
    /// <param name="run">The active owner-scoped AdminChat run.</param>
    /// <param name="liveEvent">The generation event produced by the AI service.</param>
    /// <param name="clientConnected">Whether the response connection is still writable.</param>
    /// <returns><see langword="true"/> when the client remains connected after emission.</returns>
    private async Task<bool> EmitLiveAsync(AdminChatRun run, AdminChatLiveEvent liveEvent, bool clientConnected)
    {
        switch (liveEvent.Kind)
        {
            case "token":
                return await EmitAsync(run, "token", new { text = liveEvent.Text }, clientConnected, state =>
                {
                    state.AssistantText += liveEvent.Text ?? "";
                    state.ThinkingText = "";
                    state.StatusText = "正在生成回答…";
                });
            case "usage":
                return await EmitAsync(run, "usage", liveEvent.Usage, clientConnected);
            case "assistant-phase":
                return await EmitAsync(run, "assistant-phase", liveEvent, clientConnected, state =>
                {
                    var reasoning = liveEvent.TrajectoryEvent?.EventType == "TextReasoningContent";
                    if (reasoning)
                    {
                        state.ThinkingText = liveEvent.Text ?? liveEvent.TrajectoryEvent?.Content ?? "";
                        state.StatusText = "正在思考…";
                    }
                    else if (!string.IsNullOrEmpty(liveEvent.Text))
                    {
                        state.AssistantText = liveEvent.Text;
                        state.StatusText = "正在生成回答…";
                    }
                });
            case "trajectory-event":
                return await EmitAsync(run, "trajectory-event", liveEvent, clientConnected,
                    state => state.StatusText = "正在处理任务…");
            case "async-action":
                return await EmitAsync(run, "async-action", liveEvent, clientConnected,
                    state => state.StatusText = liveEvent.ActionState?.Status switch
                    {
                        "waiting-human" => "等待人工交互…",
                        "completed" => "后台任务已完成，正在继续…",
                        "failed" => "后台任务失败，正在整理结果…",
                        "cancelled" => "后台任务已取消，正在整理结果…",
                        _ => "后台任务执行中…"
                    });
            case "trajectory-complete":
                return await EmitAsync(run, "trajectory-complete", liveEvent, clientConnected, state =>
                    state.StatusText = liveEvent.PendingApprovals?.Count > 0 ? "等待你的确认…" : "任务已完成，正在保存…");
            default:
                return clientConnected;
        }
    }

    /// <summary>
    /// Serializes and publishes one run event, records it for reconnecting observers,
    /// and writes it to the current SSE response when the client is connected.
    /// </summary>
    /// <param name="run">The active run receiving the event.</param>
    /// <param name="eventName">The SSE event name.</param>
    /// <param name="payload">The event payload to serialize.</param>
    /// <param name="clientConnected">Whether a browser response is currently writable.</param>
    /// <param name="mutate">Optional state mutation applied before the event is published.</param>
    /// <returns><see langword="true"/> when the response can still receive later events.</returns>
    private async Task<bool> EmitAsync(
        AdminChatRun run, string eventName, object payload, bool clientConnected, Action<AdminChatRun> mutate = null)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        _runs.Publish(run, eventName, json, mutate);
        if (!clientConnected || HttpContext.RequestAborted.IsCancellationRequested)
        {
            NoteDisconnect(run);
            return false;
        }

        try
        {
            await WriteRawEventAsync(eventName, json, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException)
        {
            NoteDisconnect(run);
            return false;
        }
    }

    /// <summary>
    /// Writes a pre-serialized SSE frame and flushes the response body so the
    /// browser can process the event without waiting for response buffering.
    /// </summary>
    /// <param name="eventName">The SSE event name written to the frame.</param>
    /// <param name="json">The already serialized JSON data line.</param>
    /// <param name="cancellationToken">Token used to stop the response write.</param>
    /// <exception cref="OperationCanceledException">Thrown when the write is cancelled.</exception>
    /// <exception cref="IOException">Thrown when the response connection is no longer writable.</exception>
    private async Task WriteRawEventAsync(string eventName, string json, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken).ConfigureAwait(false);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records the first observed client disconnect while leaving the server-side
    /// run active so generation and persistence can complete independently.
    /// </summary>
    /// <param name="run">The run whose browser connection was lost.</param>
    private void NoteDisconnect(AdminChatRun run)
    {
        if (Interlocked.Exchange(ref _disconnectLogged, 1) == 0)
            _logger.LogInformation("AdminChat 页面已断开，任务继续执行。SessionId={SessionId}", run.SessionId);
    }

    /// <summary>
    /// Observes a generation task after cancellation or streaming failure so that
    /// its eventual exception is consumed without masking the original request outcome.
    /// </summary>
    /// <param name="generationTask">The generation task to observe.</param>
    private static async Task ObserveGenerationAsync(Task generationTask)
    {
        try { await generationTask.ConfigureAwait(false); }
        catch (Exception)
        {
            //Legacy comment translated to English.
        }
    }

    /// <summary>
    /// Runs simple AdminChat generation, forwards token and usage events to the
    /// stream channel, and completes the channel exactly once on exit.
    /// </summary>
    /// <param name="sessionId">The owner-authorized chat session.</param>
    /// <param name="userId">The administrator requesting generation.</param>
    /// <param name="content">The user content sent to the AI service.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="writer">The channel receiving live generation events.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <param name="attachments">Validated workspace attachments for the prompt.</param>
    /// <param name="maxOutputTokens">Optional maximum output-token limit.</param>
    /// <returns>The final response text and model identifier.</returns>
    private async Task<AdminChatStreamResult> GenerateResponseAsync(
        int sessionId,
        int userId,
        string content,
        int aiModelId,
        ChannelWriter<AdminChatLiveEvent> writer,
        CancellationToken cancellationToken,
        IReadOnlyList<AdminChatAttachment> attachments,
        int maxOutputTokens)
        {
            try
            {
                var result = await _chatAiService.GenerateResponseAsync(
                    sessionId,
                    userId,
                    content,
                    aiModelId,
                    chunk => writer.TryWrite(new AdminChatLiveEvent
                    {
                        Kind = "token",
                        Text = chunk
                    }),
                    cancellationToken: cancellationToken,
                    generationOptions: maxOutputTokens > 0
                        ? new AdminChatGenerationOptions { MaxOutputTokens = maxOutputTokens }
                        : null,
                    onUsage: usage => writer.TryWrite(new AdminChatLiveEvent
                    {
                        Kind = "usage",
                        Usage = usage
                    }), attachments: attachments);
                return new AdminChatStreamResult
                {
                    Response = result.response,
                    ModelIdentifier = result.modelIdentifier
                };
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
                throw;
            }
            finally
            {
                writer.TryComplete();
            }
        }

    /// <summary>
    /// Runs plan/execute harness generation, forwards structured live events to
    /// the stream channel, and returns the final response plus harness metadata.
    /// </summary>
    /// <param name="sessionId">The owner-authorized chat session.</param>
    /// <param name="userId">The administrator requesting generation.</param>
    /// <param name="content">The user instruction sent to the harness.</param>
    /// <param name="aiModelId">The selected AI model identifier.</param>
    /// <param name="writer">The channel receiving live harness events.</param>
    /// <param name="cancellationToken">Token used to cancel generation.</param>
    /// <param name="attachments">Validated workspace attachments for the prompt.</param>
    /// <param name="requestedMode">The effective harness mode, such as plan or execute.</param>
    /// <param name="harnessOptions">Optional tool and execution options.</param>
    /// <returns>The final response, model identifier, and harness result.</returns>
    private async Task<AdminChatStreamResult> GenerateHarnessResponseAsync(
        int sessionId,
        int userId,
        string content,
        int aiModelId,
        ChannelWriter<AdminChatLiveEvent> writer,
        CancellationToken cancellationToken,
        IReadOnlyList<AdminChatAttachment> attachments,
        string requestedMode,
        AdminChatHarnessOptions harnessOptions)
    {
        try
        {
            var result = await _chatAiService.GenerateNativeHarnessResponseAsync(
                sessionId,
                userId,
                content,
                aiModelId,
                cancellationToken: cancellationToken,
                onLiveEvent: liveEvent => writer.TryWrite(liveEvent),
                attachments: attachments,
                requestedMode: requestedMode,
                harnessOptions: harnessOptions);
            return new AdminChatStreamResult
            {
                Response = result.response,
                ModelIdentifier = result.modelIdentifier,
                Harness = result.harness
            };
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
            throw;
        }
        finally
        {
            writer.TryComplete();
        }
    }

    /// <summary>
    /// Serializes an object payload as an SSE frame and flushes it to the response.
    /// This helper is retained for event paths that do not need run-state mutation.
    /// </summary>
    /// <param name="eventName">The SSE event name.</param>
    /// <param name="payload">The object serialized as the event data.</param>
    /// <param name="cancellationToken">Token used to cancel the response write.</param>
    private async Task WriteEventAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(payload, JsonOptions);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken).ConfigureAwait(false);
        await Response.WriteAsync($"data: {serialized}\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class AdminChatStreamResult
    {
        public string Response { get; set; }
        public string ModelIdentifier { get; set; }
        public AdminChatHarnessResult Harness { get; set; }
    }

    /// <summary>
    /// Reads the authenticated administrator identifier from the current claims
    /// principal and returns a negative value when it is absent or malformed.
    /// </summary>
    /// <returns>The parsed administrator identifier, or <c>-1</c> when unavailable.</returns>
    private int GetCurrentAdminUserInfoId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : -1;
    }
}
