/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HumanInTheLoopRequestStore.cs
    File description: Related implementation.
    Change description: Updated module behavior and related support.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Stores pending human-in-the-loop requests and completes them when an approval,
/// rejection, human-turn input, cancellation, or execution failure is recorded.
/// </summary>
public sealed class HumanInTheLoopRequestStore
{
    private readonly ConcurrentDictionary<string, PendingHumanRequest> _pending = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a tool approval request and returns its pending completion object.
    /// </summary>
    /// <param name="chatTaskId">The chat task waiting for the decision.</param>
    /// <param name="agentName">The agent that requested approval.</param>
    /// <param name="request">The tool approval content supplied by the model runtime.</param>
    /// <param name="responseFactory">Factory that converts a decision into the runtime response object.</param>
    /// <param name="correlationId">Optional execution correlation identifier.</param>
    /// <param name="recipientUserId">Optional user identifier that should receive the request.</param>
    /// <returns>The registered pending request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or <paramref name="responseFactory"/> is <see langword="null"/>.</exception>
    public PendingHumanRequest RegisterToolApproval(
        int chatTaskId,
        string agentName,
        ToolApprovalRequestContent request,
        Func<HumanInTheLoopDecision, object> responseFactory,
        string correlationId = null,
        string recipientUserId = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responseFactory);

        var pending = PendingHumanRequest.FromToolApproval(
            chatTaskId,
            agentName,
            request,
            responseFactory,
            correlationId,
            recipientUserId);
        Add(pending);
        return pending;
    }

    /// <summary>
    /// Registers a workflow tool approval and adapts the decision into the workflow's external response.
    /// </summary>
    /// <param name="chatTaskId">The chat task waiting for the decision.</param>
    /// <param name="agentName">The agent that requested approval.</param>
    /// <param name="externalRequest">The workflow request that must receive the decision response.</param>
    /// <param name="request">The tool approval content supplied by the workflow.</param>
    /// <param name="correlationId">Optional execution correlation identifier.</param>
    /// <param name="recipientUserId">Optional user identifier that should receive the request.</param>
    /// <returns>The registered pending request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="externalRequest"/> or <paramref name="request"/> is <see langword="null"/>.</exception>
    public PendingHumanRequest RegisterWorkflowToolApproval(
        int chatTaskId,
        string agentName,
        ExternalRequest externalRequest,
        ToolApprovalRequestContent request,
        string correlationId = null,
        string recipientUserId = null)
    {
        ArgumentNullException.ThrowIfNull(externalRequest);
        ArgumentNullException.ThrowIfNull(request);

        return RegisterToolApproval(
            chatTaskId,
            agentName,
            request,
            decision =>
            {
                var response = request.CreateResponse(decision.Approved, decision.Reason);
                return externalRequest.CreateResponse(response);
            },
            correlationId,
            recipientUserId);
    }

    /// <summary>
    /// Registers a human participant turn and waits for text input or a rejection decision.
    /// </summary>
    /// <param name="chatTaskId">The chat task waiting for human input.</param>
    /// <param name="agentName">The displayed participant name.</param>
    /// <param name="participantKey">The stable participant key used by the workflow.</param>
    /// <param name="prompt">The prompt shown to the human participant.</param>
    /// <param name="correlationId">Optional execution correlation identifier.</param>
    /// <param name="recipientUserId">Optional user identifier that should receive the request.</param>
    /// <returns>The registered pending request.</returns>
    public PendingHumanRequest RegisterHumanTurn(
        int chatTaskId,
        string agentName,
        string participantKey,
        string prompt,
        string correlationId = null,
        string recipientUserId = null)
    {
        var pending = PendingHumanRequest.FromHumanTurn(
            chatTaskId,
            agentName,
            participantKey,
            prompt,
            correlationId,
            recipientUserId);
        Add(pending);
        return pending;
    }

    /// <summary>
    /// Resolves a pending request, creates its runtime response, and completes its awaitable decision.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="decision">The approval, rejection, or human-input decision.</param>
    /// <param name="pending">Receives the removed pending request when it existed.</param>
    /// <returns><see langword="true"/> when the request was resolved and its response created.</returns>
    public bool TryResolve(string requestId, HumanInTheLoopDecision decision, out PendingHumanRequest pending)
    {
        pending = null;
        if (string.IsNullOrWhiteSpace(requestId)
            || !_pending.TryRemove(requestId.Trim(), out pending))
        {
            return false;
        }

        try
        {
            var response = pending.CreateResponse(decision);
            pending.TrySetResult(decision, response);
            return true;
        }
        catch (Exception ex)
        {
            pending.TrySetException(ex);
            return false;
        }
    }

    /// <summary>
    /// Resolves a pending request without returning the removed request object.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="decision">The approval, rejection, or human-input decision.</param>
    /// <returns><see langword="true"/> when the request was resolved.</returns>
    public bool TryResolve(string requestId, HumanInTheLoopDecision decision)
        => TryResolve(requestId, decision, out _);

    /// <summary>
    /// Looks up a pending request without removing it.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="pending">Receives the pending request when it exists.</param>
    /// <returns><see langword="true"/> when the request exists.</returns>
    public bool TryGet(string requestId, out PendingHumanRequest pending)
    {
        pending = null;
        return !string.IsNullOrWhiteSpace(requestId)
            && _pending.TryGetValue(requestId.Trim(), out pending);
    }

    /// <summary>
    /// Removes and cancels a pending request.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <returns><see langword="true"/> when a request was removed and canceled.</returns>
    public bool TryCancel(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId)
            || !_pending.TryRemove(requestId.Trim(), out var pending))
        {
            return false;
        }

        pending.TrySetCanceled();
        return true;
    }

    /// <summary>
    /// Returns pending requests for one chat task in creation order.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier.</param>
    /// <returns>A snapshot of pending request DTOs.</returns>
    public IReadOnlyList<HumanInTheLoopRequestDto> GetPending(int chatTaskId)
        => _pending.Values
            .Where(z => z.ChatTaskId == chatTaskId)
            .OrderBy(z => z.CreatedAt)
            .Select(z => z.ToDto())
            .ToList();

    /// <summary>
    /// Returns pending requests associated with a correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to match.</param>
    /// <returns>A snapshot of matching pending requests in creation order.</returns>
    public IReadOnlyList<PendingHumanRequest> GetPendingByCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Array.Empty<PendingHumanRequest>();
        }

        var normalized = correlationId.Trim();
        return _pending.Values
            .Where(z => string.Equals(z.CorrelationId, normalized, StringComparison.Ordinal))
            .OrderBy(z => z.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Cancels and removes every pending request belonging to a chat task.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier.</param>
    /// <returns>The requests that were removed and canceled.</returns>
    public IReadOnlyList<PendingHumanRequest> CancelForTask(int chatTaskId)
    {
        var cancelled = new List<PendingHumanRequest>();
        foreach (var pair in _pending.Where(z => z.Value.ChatTaskId == chatTaskId).ToList())
        {
            if (_pending.TryRemove(pair.Key, out var pending))
            {
                pending.TrySetCanceled();
                cancelled.Add(pending);
            }
        }

        return cancelled;
    }

    /// <summary>
    /// Adds a pending request to the store and rejects duplicate request identifiers.
    /// </summary>
    /// <param name="pending">The request to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when the request ID is already present.</exception>
    private void Add(PendingHumanRequest pending)
    {
        if (!_pending.TryAdd(pending.RequestId, pending))
        {
            throw new InvalidOperationException($"重复的 Human-in-the-Loop 请求 ID：{pending.RequestId}");
        }
    }
}

/// <summary>
/// Contains the human decision used to approve a tool, reject an operation, or provide turn input.
/// </summary>
public sealed record HumanInTheLoopDecision(bool Approved, string Reason = null, string Input = null);

public sealed class PendingHumanRequest
{
    private static readonly JsonSerializerOptions ToolArgumentsJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly TaskCompletionSource<HumanInTheLoopDecision> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Initializes a pending human-interaction request.
    /// </summary>
    /// <param name="chatTaskId">The owning chat-task identifier.</param>
    /// <param name="agentName">The Agent display name.</param>
    /// <param name="requestType">The interaction type.</param>
    /// <param name="toolName">The requested tool name.</param>
    /// <param name="toolArguments">Serialized tool arguments.</param>
    /// <param name="prompt">The human-facing prompt.</param>
    /// <param name="participantKey">The participant key for a human turn.</param>
    /// <param name="responseFactory">The response factory used after approval.</param>
    /// <param name="correlationId">Optional run correlation identifier.</param>
    /// <param name="recipientUserId">Optional notification recipient.</param>
    private PendingHumanRequest(
        int chatTaskId,
        string agentName,
        string requestType,
        string toolName,
        string toolArguments,
        string prompt,
        string participantKey,
        Func<HumanInTheLoopDecision, object> responseFactory,
        string correlationId,
        string recipientUserId)
    {
        ChatTaskId = chatTaskId;
        AgentName = agentName ?? string.Empty;
        RequestType = requestType;
        ToolName = toolName ?? string.Empty;
        ToolArguments = toolArguments ?? string.Empty;
        Prompt = prompt ?? string.Empty;
        ParticipantKey = participantKey ?? string.Empty;
        CorrelationId = correlationId?.Trim() ?? string.Empty;
        RecipientUserId = recipientUserId?.Trim() ?? string.Empty;
        CreateResponse = responseFactory;
        RequestId = Guid.NewGuid().ToString("n");
        CreatedAt = DateTimeOffset.Now;
    }

    public string RequestId { get; }
    public int ChatTaskId { get; }
    public string AgentName { get; }
    public string RequestType { get; }
    public string ToolName { get; }
    public string ToolArguments { get; }
    public string Prompt { get; }
    public string ParticipantKey { get; }
    public string CorrelationId { get; }
    public string RecipientUserId { get; }
    public string NeuBellItemId { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public Task<HumanInTheLoopDecision> Completion => _completion.Task;
    internal Func<HumanInTheLoopDecision, object> CreateResponse { get; }

    public object ResolvedResponse { get; private set; }

    /// <summary>
    /// Creates a pending request from an AI tool approval request.
    /// </summary>
    /// <param name="chatTaskId">The owning chat-task identifier.</param>
    /// <param name="agentName">The Agent display name.</param>
    /// <param name="request">The tool approval content.</param>
    /// <param name="responseFactory">The response factory used after approval.</param>
    /// <param name="correlationId">Optional run correlation identifier.</param>
    /// <param name="recipientUserId">Optional notification recipient.</param>
    /// <returns>A pending tool-approval request.</returns>
    public static PendingHumanRequest FromToolApproval(
        int chatTaskId,
        string agentName,
        ToolApprovalRequestContent request,
        Func<HumanInTheLoopDecision, object> responseFactory,
        string correlationId = null,
        string recipientUserId = null)
    {
        var functionCall = request.ToolCall as FunctionCallContent;
        var arguments = functionCall?.Arguments == null
            ? string.Empty
            : JsonSerializer.Serialize(functionCall.Arguments, ToolArgumentsJsonOptions);

        return new PendingHumanRequest(
            chatTaskId,
            agentName,
            "toolApproval",
            functionCall?.Name,
            arguments,
            $"工具“{functionCall?.Name ?? request.ToolCall.GetType().Name}”请求人工确认后执行。",
            string.Empty,
            responseFactory,
            correlationId,
            recipientUserId);
    }

    /// <summary>
    /// Creates a pending request for direct human input during a ChatTask.
    /// </summary>
    /// <param name="chatTaskId">The owning chat-task identifier.</param>
    /// <param name="agentName">The Agent display name.</param>
    /// <param name="participantKey">The participant key.</param>
    /// <param name="prompt">The input prompt shown to the human.</param>
    /// <param name="correlationId">Optional run correlation identifier.</param>
    /// <param name="recipientUserId">Optional notification recipient.</param>
    /// <returns>A pending human-turn request.</returns>
    public static PendingHumanRequest FromHumanTurn(
        int chatTaskId,
        string agentName,
        string participantKey,
        string prompt,
        string correlationId = null,
        string recipientUserId = null)
    {
        return new PendingHumanRequest(
            chatTaskId,
            agentName,
            "humanTurn",
            string.Empty,
            string.Empty,
            prompt,
            participantKey,
            decision => decision.Input ?? string.Empty,
            correlationId,
            recipientUserId);
    }

    /// <summary>
    /// Associates a NeuBell notification identifier with this pending request.
    /// </summary>
    /// <param name="itemId">The notification identifier; blank input clears it.</param>
    public void SetNeuBellItemId(string itemId)
    {
        NeuBellItemId = string.IsNullOrWhiteSpace(itemId) ? null : itemId.Trim();
    }

    /// <summary>
    /// Completes the request with a human decision and generated response.
    /// </summary>
    /// <param name="decision">The human decision.</param>
    /// <param name="response">The response object returned to the Agent runtime.</param>
    internal void TrySetResult(HumanInTheLoopDecision decision, object response)
    {
        ResolvedResponse = response;
        _completion.TrySetResult(decision);
    }

    /// <summary>
    /// Completes the request with an execution exception.
    /// </summary>
    /// <param name="exception">The exception observed during interaction.</param>
    internal void TrySetException(Exception exception)
        => _completion.TrySetException(exception);

    /// <summary>
    /// Completes the request as canceled.
    /// </summary>
    internal void TrySetCanceled()
        => _completion.TrySetCanceled();

    /// <summary>
    /// Projects the pending request into the client-facing DTO without exposing response factories.
    /// </summary>
    /// <returns>The transport DTO.</returns>
    public HumanInTheLoopRequestDto ToDto()
        => new()
        {
            RequestId = RequestId,
            ChatTaskId = ChatTaskId,
            AgentName = AgentName,
            RequestType = RequestType,
            ToolName = ToolName,
            ToolArguments = ToolArguments,
            Prompt = Prompt,
            ParticipantKey = ParticipantKey,
            CorrelationId = CorrelationId,
            NeuBellItemId = NeuBellItemId,
            CreatedAt = CreatedAt
        };
}

public sealed class HumanInTheLoopRequestDto
{
    public string RequestId { get; set; }
    public int ChatTaskId { get; set; }
    public string AgentName { get; set; }
    public string RequestType { get; set; }
    public string ToolName { get; set; }
    public string ToolArguments { get; set; }
    public string Prompt { get; set; }
    public string ParticipantKey { get; set; }
    public string CorrelationId { get; set; }
    public string NeuBellItemId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
