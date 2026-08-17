/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HumanParticipantAgent.cs
    文件功能描述：把文本 Human 回合适配为 MAF AIAgent

    创建标识：Senparc - 20260815
    修改描述：Human 轮次进入工作流后暂停，待 HTTP 回复再恢复

    修改标识：Senparc - 20260817
    修改描述：v0.16.0-preview21 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class HumanParticipantAgent : AIAgent
{
    private sealed class HumanAgentSession : AgentSession
    {
        public HumanAgentSession() : base() { }
        public HumanAgentSession(AgentSessionStateBag stateBag) : base(stateBag) { }
    }

    private readonly HumanInTheLoopRequestStore _requestStore;
    private readonly int _chatTaskId;
    private readonly string _participantKey;
    private readonly string _correlationId;
    private readonly string _recipientUserId;
    private readonly Func<PendingHumanRequest, Task> _onRequestCreated;
    private readonly Func<PendingHumanRequest, HumanInTheLoopDecision, Task> _onRequestResolved;

    public HumanParticipantAgent(
        HumanInTheLoopRequestStore requestStore,
        int chatTaskId,
        string participantKey,
        Func<PendingHumanRequest, Task> onRequestCreated,
        Func<PendingHumanRequest, HumanInTheLoopDecision, Task> onRequestResolved,
        string correlationId = null,
        string recipientUserId = null)
    {
        _requestStore = requestStore ?? throw new ArgumentNullException(nameof(requestStore));
        _chatTaskId = chatTaskId;
        _participantKey = string.IsNullOrWhiteSpace(participantKey)
            ? HumanParticipantConstants.ParticipantKey
            : participantKey;
        _correlationId = correlationId;
        _recipientUserId = recipientUserId;
        _onRequestCreated = onRequestCreated;
        _onRequestResolved = onRequestResolved;
    }

    public override string Name => HumanParticipantConstants.Name;

    public override string Description => "等待当前用户输入文本的 Human-in-the-Loop 参与者。";

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new HumanAgentSession());

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }, serializerOptions));

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new HumanAgentSession());

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession session,
        AgentRunOptions options,
        CancellationToken cancellationToken)
    {
        var input = await WaitForHumanInputAsync(messages, cancellationToken).ConfigureAwait(false);
        return new AgentResponse(new ChatMessage(ChatRole.Assistant, input));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession session,
        AgentRunOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var input = await WaitForHumanInputAsync(messages, cancellationToken).ConfigureAwait(false);
        yield return new AgentResponseUpdate(ChatRole.Assistant, input);
    }

    private async Task<string> WaitForHumanInputAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var prompt = BuildPrompt(messages);
        var pending = _requestStore.RegisterHumanTurn(
            _chatTaskId,
            Name,
            _participantKey,
            prompt,
            _correlationId,
            _recipientUserId);

        if (_onRequestCreated != null)
        {
            await _onRequestCreated(pending).ConfigureAwait(false);
        }

        var decision = await pending.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (_onRequestResolved != null)
        {
            await _onRequestResolved(pending, decision).ConfigureAwait(false);
        }

        if (!decision.Approved || string.IsNullOrWhiteSpace(decision.Input))
        {
            throw new InvalidOperationException("Human 回合必须提交非空文本；如果用户拒绝，请停止当前任务。");
        }

        return decision.Input.Trim();
    }

    private static string BuildPrompt(IEnumerable<ChatMessage> messages)
    {
        var latest = messages?
            .Reverse()
            .Select(ExtractText)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        return string.IsNullOrWhiteSpace(latest)
            ? "当前轮次等待 Human 输入文本。"
            : $"当前轮次等待 Human 输入文本。上一位参与者的消息：\n{latest}";
    }

    private static string ExtractText(ChatMessage message)
        => message?.Text?.Trim() ?? string.Empty;
}
