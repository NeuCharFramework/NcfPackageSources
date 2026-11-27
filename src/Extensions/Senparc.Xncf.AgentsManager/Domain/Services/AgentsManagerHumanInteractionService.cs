/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerHumanInteractionService.cs
    File description: Related implementation.
    Change description: Updated module behavior and related support.

    Created by: Senparc - 20260815

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.Trace;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Represents the outcome of resolving an AgentsManager human interaction request.
/// </summary>
public sealed record AgentsManagerHumanInteractionResolution(
    bool Success,
    PendingHumanRequest Pending,
    HumanInTheLoopDecision Decision,
    string Message);

/// <summary>
/// Coordinates pending Human-in-the-Loop and tool-approval requests for AgentsManager
/// and exposes them through the Workflow interaction bridge.
/// </summary>
public sealed class AgentsManagerHumanInteractionService
{
    private readonly HumanInTheLoopRequestStore _requestStore;
    private readonly AgentsManagerNeuBellProvider _neuBellProvider;

    /// <summary>
    /// Initializes the human-interaction coordinator.
    /// </summary>
    /// <param name="requestStore">Store for pending and resolved requests.</param>
    /// <param name="neuBellProvider">Provider used to consume approval notifications.</param>
    public AgentsManagerHumanInteractionService(
        HumanInTheLoopRequestStore requestStore,
        AgentsManagerNeuBellProvider neuBellProvider)
    {
        _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
        _neuBellProvider = neuBellProvider ?? throw new ArgumentNullException(nameof(neuBellProvider));
    }

    /// <summary>
    /// Lists accessible pending requests for a correlation ID and user.
    /// </summary>
    /// <param name="correlationId">The workflow/task correlation ID.</param>
    /// <param name="userId">The requesting user identifier.</param>
    /// <returns>Pending requests visible to the user.</returns>
    public IReadOnlyList<PendingHumanRequest> GetPending(string correlationId, string userId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Array.Empty<PendingHumanRequest>();
        }

        return _requestStore
            .GetPendingByCorrelationId(correlationId)
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            .Where(request => !string.Equals(request.RequestType, "humanTurn", StringComparison.Ordinal)
                              || !string.IsNullOrWhiteSpace(request.NeuBellItemId))
            .Where(request => CanAccess(request, userId))
            .ToList();
    }

    /// <summary>
    /// Validates and resolves one Human-in-the-Loop or tool-approval request,
    /// consuming its NeuBell item when resolution succeeds.
    /// </summary>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="userId">The resolving user identifier.</param>
    /// <param name="decision">The approval/rejection decision and optional input.</param>
    /// <param name="correlationId">Optional correlation ID boundary.</param>
    /// <param name="cancellationToken">Token used to cancel notification consumption.</param>
    /// <returns>The resolution result.</returns>
    public async Task<AgentsManagerHumanInteractionResolution> ResolveAsync(
        string requestId,
        string userId,
        HumanInTheLoopDecision decision,
        string correlationId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (decision == null)
        {
            return Failure("缺少人工处理结果。");
        }

        if (!_requestStore.TryGet(requestId, out var pending))
        {
            return Failure("人工请求不存在、已处理或已失效。");
        }

        if (!string.IsNullOrWhiteSpace(correlationId)
            && !string.Equals(pending.CorrelationId, correlationId.Trim(), StringComparison.Ordinal))
        {
            return Failure("人工请求不属于当前 Workflow 运行。");
        }

        if (!CanAccess(pending, userId))
        {
            return Failure("当前账号无权处理该人工请求。");
        }

        if (string.Equals(pending.RequestType, "humanTurn", StringComparison.Ordinal)
            && (!decision.Approved || string.IsNullOrWhiteSpace(decision.Input)))
        {
            return Failure("Human 回合必须提交非空文本。");
        }

        if (!_requestStore.TryResolve(requestId, decision, out var resolvedPending))
        {
            return Failure("人工请求不存在、已被其他入口处理或已失效。");
        }

        if (string.Equals(resolvedPending.RequestType, "toolApproval", StringComparison.Ordinal))
        {
            SenparcTrace.SendCustomLog(
                "AgentsManager.HIL.ToolApproval.Resolved",
                $"Task={resolvedPending.ChatTaskId}; Correlation={resolvedPending.CorrelationId}; " +
                $"Request={resolvedPending.RequestId}; Tool={resolvedPending.ToolName}; " +
                $"Approved={decision.Approved}; Reason={decision.Reason}");
        }

        if (!string.IsNullOrWhiteSpace(resolvedPending.NeuBellItemId))
        {
            await _neuBellProvider.ConsumeItemAsync(
                new NeuBellRequestContext(userId),
                resolvedPending.NeuBellItemId,
                cancellationToken).ConfigureAwait(false);
            await NotifyNeuBellChangedAsync().ConfigureAwait(false);
        }

        return new AgentsManagerHumanInteractionResolution(
            true,
            resolvedPending,
            decision,
            "人工处理已提交，任务继续执行。");
    }

    /// <summary>
    /// Converts accessible pending requests into Workflow interaction DTOs.
    /// </summary>
    /// <param name="correlationId">The workflow correlation ID.</param>
    /// <param name="userId">The requesting user identifier.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>A value task containing workflow interactions.</returns>
    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetWorkflowPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var interactions = GetPending(correlationId, userId)
            .Select(request => new WorkflowHumanInteraction(
                request.RequestId,
                request.ChatTaskId,
                request.CorrelationId,
                request.RequestType,
                request.AgentName,
                request.ToolName,
                request.ToolArguments,
                request.Prompt,
                request.ParticipantKey,
                request.NeuBellItemId,
                request.CreatedAt))
            .ToList();
        return ValueTask.FromResult<IReadOnlyList<WorkflowHumanInteraction>>(interactions);
    }

    /// <summary>
    /// Resolves a Workflow interaction through the AgentsManager request store.
    /// </summary>
    /// <param name="correlationId">The workflow correlation ID.</param>
    /// <param name="userId">The resolving user identifier.</param>
    /// <param name="requestId">The pending request identifier.</param>
    /// <param name="approved">Whether the request is approved.</param>
    /// <param name="input">Optional human input.</param>
    /// <param name="reason">Optional rejection/approval reason.</param>
    /// <param name="cancellationToken">Token used to cancel notification consumption.</param>
    /// <returns>A value task containing the Workflow resolution result.</returns>
    public async ValueTask<WorkflowHumanInteractionResult> ResolveWorkflowAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string input = null,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveAsync(
            requestId,
            userId,
            new HumanInTheLoopDecision(approved, reason, input),
            correlationId,
            cancellationToken).ConfigureAwait(false);
        return new WorkflowHumanInteractionResult(
            resolution.Success,
            resolution.Decision?.Approved ?? false,
            resolution.Decision?.Input,
            resolution.Decision?.Reason,
            resolution.Message);
    }

    /// <summary>Checks whether a user may access a pending request.</summary>
    /// <param name="pending">The pending request.</param>
    /// <param name="userId">The candidate user identifier.</param>
    /// <returns><see langword="true"/> when the request is unassigned or assigned to the user.</returns>
    private static bool CanAccess(PendingHumanRequest pending, string userId)
    {
        if (pending == null)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(pending.RecipientUserId)
            || string.Equals(
                pending.RecipientUserId,
                userId?.Trim(),
                StringComparison.Ordinal);
    }

    /// <summary>Creates a failed interaction-resolution result.</summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed resolution result.</returns>
    private static AgentsManagerHumanInteractionResolution Failure(string message)
        => new(false, null, null, message);

    /// <summary>Notifies NeuBell subscribers after an interaction notification is consumed.</summary>
    private static async Task NotifyNeuBellChangedAsync()
    {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        INeuBellPublisher publisher = null;
        try
        {
            publisher = SenparcDI.GetServiceProvider(true).GetService<INeuBellPublisher>();
        }
        catch
        {
            //Legacy comment translated to English.
        }

        if (publisher != null)
        {
            await publisher.NotifyChangedAsync(AgentsManagerNeuBellProvider.ProviderIdValue).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Adapts AgentsManager human-interaction operations to the Workflow bridge contract.
/// </summary>
public sealed class AgentsManagerWorkflowHumanInteractionBridge : IWorkflowHumanInteractionBridge
{
    private readonly AgentsManagerHumanInteractionService _service;

    /// <summary>
    /// Initializes the Workflow bridge adapter.
    /// </summary>
    /// <param name="service">The AgentsManager interaction service.</param>
    public AgentsManagerWorkflowHumanInteractionBridge(AgentsManagerHumanInteractionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Lists pending interactions through the underlying AgentsManager service.</summary>
    /// <param name="correlationId">The workflow correlation ID.</param><param name="userId">The requesting user.</param><param name="cancellationToken">Cancellation token.</param>
    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
        => _service.GetWorkflowPendingAsync(correlationId, userId, cancellationToken);

    /// <summary>Resolves an interaction through the underlying AgentsManager service.</summary>
    /// <param name="correlationId">The workflow correlation ID.</param><param name="userId">The resolving user.</param><param name="requestId">The request ID.</param><param name="approved">Approval state.</param><param name="input">Optional input.</param><param name="reason">Optional reason.</param><param name="cancellationToken">Cancellation token.</param>
    public ValueTask<WorkflowHumanInteractionResult> ResolveAsync(
        string correlationId,
        string userId,
        string requestId,
        bool approved,
        string input = null,
        string reason = null,
        CancellationToken cancellationToken = default)
        => _service.ResolveWorkflowAsync(
            correlationId,
            userId,
            requestId,
            approved,
            input,
            reason,
            cancellationToken);
}
