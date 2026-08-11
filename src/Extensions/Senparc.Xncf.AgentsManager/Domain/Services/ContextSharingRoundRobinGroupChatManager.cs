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
/// 控制 GroupChat 工作流每轮向参与者广播的上下文。
/// Microsoft Agent Framework 的 GroupChatManager 会保留完整 canonical history 用于编排，
/// 此类只收缩投递给每个参与者会话的 payload，避免把工具调用、原始表示或长历史带出边界。
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

    protected override ValueTask<IEnumerable<ChatMessage>> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (_contextSharingMode == ChatGroupContextSharingMode.LegacyFullHistory)
        {
            return base.UpdateHistoryAsync(history, cancellationToken);
        }

        if (history == null || history.Count == 0)
        {
            return ValueTask.FromResult<IEnumerable<ChatMessage>>(Array.Empty<ChatMessage>());
        }

        if (_contextSharingMode == ChatGroupContextSharingMode.InstructionOnly)
        {
            // 第一轮必须保留用户指令；后续轮次只发送协调提示，不转发任何其他 Agent 的原文。
            var instruction = history.LastOrDefault(z => z.Role == ChatRole.User);
            if (instruction != null)
            {
                return ValueTask.FromResult<IEnumerable<ChatMessage>>(new[] { CreateTextOnlyMessage(instruction, MaxSharedConclusionLength) });
            }

            return ValueTask.FromResult<IEnumerable<ChatMessage>>(new[]
            {
                new ChatMessage(ChatRole.User,
                    "协作已进入下一轮。请只依据初始任务和你的职责继续工作，并输出不含推理过程的简短共享结论。")
            });
        }

        // InstructionAndKeyReplies：默认远程模式。只投递当前轮的纯文本、截断后的共享结论；
        // 丢弃 RawRepresentation、工具调用、usage、结构化内容和此前历史。
        return ValueTask.FromResult<IEnumerable<ChatMessage>>(
            history.Select(z => CreateTextOnlyMessage(z, MaxSharedConclusionLength)).ToList());
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
