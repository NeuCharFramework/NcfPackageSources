/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AgentsManagerHumanInteractionService.cs
    File description: Related implementation.

    Created by: Senparc - 20260815
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260817
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

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

public sealed record AgentsManagerHumanInteractionResolution(
    bool Success,
    PendingHumanRequest Pending,
    HumanInTheLoopDecision Decision,
    string Message);

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class AgentsManagerHumanInteractionService
{
    private readonly HumanInTheLoopRequestStore _requestStore;
    private readonly AgentsManagerNeuBellProvider _neuBellProvider;

    public AgentsManagerHumanInteractionService(
        HumanInTheLoopRequestStore requestStore,
        AgentsManagerNeuBellProvider neuBellProvider)
    {
        _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
        _neuBellProvider = neuBellProvider ?? throw new ArgumentNullException(nameof(neuBellProvider));
    }

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

    private static AgentsManagerHumanInteractionResolution Failure(string message)
        => new(false, null, null, message);

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
///Legacy comment translated to English.
/// </summary>
public sealed class AgentsManagerWorkflowHumanInteractionBridge : IWorkflowHumanInteractionBridge
{
    private readonly AgentsManagerHumanInteractionService _service;

    public AgentsManagerWorkflowHumanInteractionBridge(AgentsManagerHumanInteractionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public ValueTask<IReadOnlyList<WorkflowHumanInteraction>> GetPendingAsync(
        string correlationId,
        string userId,
        CancellationToken cancellationToken = default)
        => _service.GetWorkflowPendingAsync(correlationId, userId, cancellationToken);

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
