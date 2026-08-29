/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ContextSharingRoundRobinGroupChatManager.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260812

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260829
    修改描述：v0.17.0 增强 Agent 请求诊断、ChatGroup 状态处理与工作流对象支持

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

    protected override async ValueTask<IEnumerable<ChatMessage>> UpdateHistoryAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        if (_contextSharingMode == ChatGroupContextSharingMode.LegacyFullHistory)
        {
            var fullHistory = await base.UpdateHistoryAsync(history, cancellationToken).ConfigureAwait(false);
            return RemoveApprovalProtocolContent(fullHistory);
        }

        if (history == null || history.Count == 0)
        {
            return Array.Empty<ChatMessage>();
        }

        if (_contextSharingMode == ChatGroupContextSharingMode.InstructionOnly)
        {
            // 第一轮必须保留用户指令；后续轮次只发送协调提示，不转发任何其他 Agent 的原文。
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

        // InstructionAndKeyReplies：默认远程模式。只投递当前轮的纯文本、截断后的共享结论；
        // 丢弃 RawRepresentation、工具调用、usage、结构化内容和此前历史。
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
