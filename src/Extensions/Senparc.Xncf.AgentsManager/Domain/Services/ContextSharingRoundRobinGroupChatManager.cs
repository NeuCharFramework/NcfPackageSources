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
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
internal sealed class ContextSharingRoundRobinGroupChatManager : RoundRobinGroupChatManager
{
    private const int MaxSharedConclusionLength = 2400;
    private readonly ChatGroupContextSharingMode _contextSharingMode;

    public ContextSharingRoundRobinGroupChatManager(
        IReadOnlyList<AIAgent> agents,
        ChatGroupContextSharingMode contextSharingMode,
        Func<RoundRobinGroupChatManager, IEnumerable<ChatMessage>, CancellationToken, ValueTask<bool>> shouldTerminateFunc)
        : base(agents, shouldTerminateFunc)
    {
        _contextSharingMode = contextSharingMode;
    }

    protected override async ValueTask<IEnumerable<ChatMessage>> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (_contextSharingMode == ChatGroupContextSharingMode.LegacyFullHistory)
        {
            //Legacy comment translated to English.
            var currentBroadcast = await base.UpdateHistoryAsync(history, cancellationToken).ConfigureAwait(false);
            return RemoveApprovalProtocolContent(currentBroadcast);
        }

        if (history == null || history.Count == 0)
        {
            return Array.Empty<ChatMessage>();
        }

        if (_contextSharingMode == ChatGroupContextSharingMode.InstructionOnly)
        {
            //Legacy comment translated to English.
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

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        return history.Select(z => CreateTextOnlyMessage(z, MaxSharedConclusionLength)).ToList();
    }

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
