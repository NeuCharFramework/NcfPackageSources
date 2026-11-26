/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ContextSharingRoundRobinGroupChatManager.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.19.3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Applies the selected ChatGroup context-sharing policy to each round-robin
/// broadcast while removing approval protocol content from shared history.
/// </summary>
internal sealed class ContextSharingRoundRobinGroupChatManager : RoundRobinGroupChatManager
{
    private const int MaxSharedConclusionLength = 2400;
    private readonly ChatGroupContextSharingMode _contextSharingMode;

    /// <summary>
    /// Initializes a round-robin manager with the selected history-sharing policy.
    /// </summary>
    /// <param name="agents">The agents participating in the group.</param>
    /// <param name="contextSharingMode">The policy controlling what history is broadcast.</param>
    /// <param name="shouldTerminateFunc">The termination predicate used by the base manager.</param>
    public ContextSharingRoundRobinGroupChatManager(
        IReadOnlyList<AIAgent> agents,
        ChatGroupContextSharingMode contextSharingMode,
        Func<RoundRobinGroupChatManager, IEnumerable<ChatMessage>, CancellationToken, ValueTask<bool>> shouldTerminateFunc)
        : base(agents, shouldTerminateFunc)
    {
        _contextSharingMode = contextSharingMode;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Applies the configured sharing mode, removes approval protocol content,
    /// and bounds shared text conclusions.
    /// </remarks>
    protected override async ValueTask<IEnumerable<ChatMessage>> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (_contextSharingMode == ChatGroupContextSharingMode.LegacyFullHistory)
        {
            // Legacy full-history mode preserves the parent manager's broadcast behavior.
            var currentBroadcast = await base.UpdateHistoryAsync(history, cancellationToken).ConfigureAwait(false);
            return RemoveApprovalProtocolContent(currentBroadcast);
        }

        if (history == null || history.Count == 0)
        {
            return Array.Empty<ChatMessage>();
        }

        if (_contextSharingMode == ChatGroupContextSharingMode.InstructionOnly)
        {
            // Instruction-only mode shares only the latest user instruction.
            var instruction = history.LastOrDefault(z => z.Role == ChatRole.User);
            if (instruction != null)
            {
                return new[] { CreateTextOnlyMessage(instruction, MaxSharedConclusionLength) };
            }

            return new[]
            {
                new ChatMessage(ChatRole.User,
                    "协作已进入下一轮。请只依据初始任务和你的职责继续工作，并输出不含推理过程的简短共享结论。")
            };
        }

        // Default mode shares bounded text-only conclusions to control context size.
        return history.Select(z => CreateTextOnlyMessage(z, MaxSharedConclusionLength)).ToList();
    }

    /// <summary>
    /// Removes tool approval request/response contents while retaining ordinary message metadata.
    /// </summary>
    /// <param name="history">The history sequence to filter.</param>
    /// <returns>Messages without approval protocol content.</returns>
    private static IEnumerable<ChatMessage> RemoveApprovalProtocolContent(
        IEnumerable<ChatMessage> history)
    {
        foreach (var message in history ?? Array.Empty<ChatMessage>())
        {
            var contents = message.Contents?
                .Where(content =>
                    content is not ToolApprovalRequestContent
                    && content is not ToolApprovalResponseContent)
                .ToList()
                ?? new List<AIContent>();

            if (contents.Count == 0)
            {
                continue;
            }

            if (message.Contents != null && contents.Count == message.Contents.Count)
            {
                yield return message;
                continue;
            }

            yield return new ChatMessage(message.Role, contents)
            {
                AuthorName = message.AuthorName,
                MessageId = message.MessageId,
                CreatedAt = message.CreatedAt,
                AdditionalProperties = message.AdditionalProperties == null
                    ? null
                    : new AdditionalPropertiesDictionary(message.AdditionalProperties)
            };
        }
    }

    /// <summary>
    /// Projects a message to bounded text-only content for cross-agent history sharing.
    /// </summary>
    /// <param name="source">The original message.</param>
    /// <param name="maxLength">The maximum shared text length.</param>
    /// <returns>A text-only message retaining the source role and metadata.</returns>
    private static ChatMessage CreateTextOnlyMessage(ChatMessage source, int maxLength)
    {
        var text = string.Join(Environment.NewLine, source.Contents?
            .OfType<TextContent>()
            .Select(z => z.Text)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            ?? Array.Empty<string>());

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "本轮没有可共享的文本结论。";
        }
        else if (text.Length > maxLength)
        {
            text = text[..maxLength] + "\n[共享结论已截断]";
        }

        return new ChatMessage(source.Role, text);
    }
}
