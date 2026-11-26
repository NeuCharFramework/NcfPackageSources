/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HumanParticipantAgent.cs
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
        /// <summary>
        /// Initializes an empty human-agent session.
        /// </summary>
        public HumanAgentSession() : base() { }
        /// <summary>
        /// Restores a human-agent session from serialized state.
        /// </summary>
        /// <param name="stateBag">The persisted Agent session state.</param>
        public HumanAgentSession(AgentSessionStateBag stateBag) : base(stateBag) { }
    }

    private readonly HumanInTheLoopRequestStore _requestStore;
    private readonly int _chatTaskId;
    private readonly string _participantKey;
    private readonly string _correlationId;
    private readonly string _recipientUserId;
    private readonly Func<PendingHumanRequest, Task> _onRequestCreated;
    private readonly Func<PendingHumanRequest, HumanInTheLoopDecision, Task> _onRequestResolved;

    /// <summary>
    /// Initializes the Human-in-the-Loop participant with request storage and lifecycle callbacks.
    /// </summary>
    /// <param name="requestStore">The store used to register and resolve human requests.</param>
    /// <param name="chatTaskId">The owning chat-task identifier.</param>
    /// <param name="participantKey">The participant key, or the default human key when blank.</param>
    /// <param name="onRequestCreated">The callback invoked after a request is created.</param>
    /// <param name="onRequestResolved">The callback invoked after a decision is received.</param>
    /// <param name="correlationId">Optional run correlation identifier.</param>
    /// <param name="recipientUserId">Optional notification recipient.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestStore"/> is null.</exception>
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

    /// <inheritdoc/>
    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new HumanAgentSession());

    /// <inheritdoc/>
    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }, serializerOptions));

    /// <inheritdoc/>
    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new HumanAgentSession());

    /// <inheritdoc/>
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession session,
        AgentRunOptions options,
        CancellationToken cancellationToken)
    {
        var input = await WaitForHumanInputAsync(messages, cancellationToken).ConfigureAwait(false);
        return new AgentResponse(new ChatMessage(ChatRole.Assistant, input));
    }

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession session,
        AgentRunOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var input = await WaitForHumanInputAsync(messages, cancellationToken).ConfigureAwait(false);
        yield return new AgentResponseUpdate(ChatRole.Assistant, input);
    }

    /// <summary>
    /// Registers a human-turn request, waits for its decision, and returns approved input.
    /// </summary>
    /// <param name="messages">The preceding conversation messages used to build the prompt.</param>
    /// <param name="cancellationToken">The wait cancellation token.</param>
    /// <returns>The trimmed approved human input.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the decision is rejected or input is blank.</exception>
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

    /// <summary>
    /// Builds the human-facing prompt from the latest non-empty message.
    /// </summary>
    /// <param name="messages">The preceding conversation messages.</param>
    /// <returns>The prompt displayed to the human participant.</returns>
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

    /// <summary>
    /// Extracts trimmed text from one chat message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <returns>The trimmed message text or an empty string.</returns>
    private static string ExtractText(ChatMessage message)
        => message?.Text?.Trim() ?? string.Empty;
}
