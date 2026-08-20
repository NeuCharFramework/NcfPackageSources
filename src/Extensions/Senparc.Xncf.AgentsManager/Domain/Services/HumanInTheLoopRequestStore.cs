/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HumanInTheLoopRequestStore.cs
    文件功能描述：AgentsManager 进程内 Human-in-the-Loop 请求暂存与恢复

    创建标识：Senparc - 20260815
    修改描述：为 MAF/AgentKernel 工具审批提供跨 HTTP 请求的等待句柄

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 AgentTemplate 模型绑定、空输出 Token 重试与 Human-in-the-Loop

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
/// AgentsManager 的进程内人工审批请求存储。
/// 该存储只负责把一个正在等待的 MAF/AgentKernel 请求与后续 HTTP 决策关联起来；
/// 进程重启或多实例部署时需要后续接入持久化 checkpoint/共享协调器。
/// </summary>
public sealed class HumanInTheLoopRequestStore
{
    private readonly ConcurrentDictionary<string, PendingHumanRequest> _pending = new(StringComparer.Ordinal);

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

    public bool TryResolve(string requestId, HumanInTheLoopDecision decision)
        => TryResolve(requestId, decision, out _);

    public bool TryGet(string requestId, out PendingHumanRequest pending)
    {
        pending = null;
        return !string.IsNullOrWhiteSpace(requestId)
            && _pending.TryGetValue(requestId.Trim(), out pending);
    }

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

    public IReadOnlyList<HumanInTheLoopRequestDto> GetPending(int chatTaskId)
        => _pending.Values
            .Where(z => z.ChatTaskId == chatTaskId)
            .OrderBy(z => z.CreatedAt)
            .Select(z => z.ToDto())
            .ToList();

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

    private void Add(PendingHumanRequest pending)
    {
        if (!_pending.TryAdd(pending.RequestId, pending))
        {
            throw new InvalidOperationException($"重复的 Human-in-the-Loop 请求 ID：{pending.RequestId}");
        }
    }
}

public sealed record HumanInTheLoopDecision(bool Approved, string Reason = null, string Input = null);

public sealed class PendingHumanRequest
{
    private static readonly JsonSerializerOptions ToolArgumentsJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly TaskCompletionSource<HumanInTheLoopDecision> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

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

    public void SetNeuBellItemId(string itemId)
    {
        NeuBellItemId = string.IsNullOrWhiteSpace(itemId) ? null : itemId.Trim();
    }

    internal void TrySetResult(HumanInTheLoopDecision decision, object response)
    {
        ResolvedResponse = response;
        _completion.TrySetResult(decision);
    }

    internal void TrySetException(Exception exception)
        => _completion.TrySetException(exception);

    internal void TrySetCanceled()
        => _completion.TrySetCanceled();

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
