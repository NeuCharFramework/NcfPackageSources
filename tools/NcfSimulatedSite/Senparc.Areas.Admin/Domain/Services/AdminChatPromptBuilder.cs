/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AdminChatPromptBuilder.cs
    文件功能描述：后台 AI 对话提示词构建器


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.11.0 增强后台 Admin Chat 群组配置、提示词构建与上下文控制

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Areas.Admin.Domain.Services;

public static class AdminChatPromptBuilder
{
    public const int MaxHistoryCharacters = 7500;
    public const int MaxCurrentMessageCharacters = 7500;
    public const int MaxUserPromptCharacters = 16000;

    public static string BuildUserPrompt(IReadOnlyList<AdminChatMessage> messages, string currentUserMessage)
    {
        var history = string.Join("\n", (messages ?? Array.Empty<AdminChatMessage>())
            .OrderBy(message => message.Sequence)
            .TakeLast(12)
            .Select(message => $"[{GetRoleName(message.RoleType)}] {message.Content}"));
        if (history.Length > MaxHistoryCharacters)
        {
            history = $"[较早对话已省略]\n{history.Substring(history.Length - MaxHistoryCharacters + 10)}";
        }

        var currentMessage = currentUserMessage ?? string.Empty;
        if (currentMessage.Length > MaxCurrentMessageCharacters)
        {
            currentMessage = currentMessage.Substring(0, MaxCurrentMessageCharacters - 10) + "…[内容已截断]";
        }

        var prompt = "以下是最近对话上下文，请在保持语义连贯的前提下回答最后一个用户问题。\n\n"
             + history
             + $"\n\n[用户当前问题] {currentMessage}";

        return prompt.Length <= MaxUserPromptCharacters
            ? prompt
            : prompt.Substring(prompt.Length - MaxUserPromptCharacters);
    }

    private static string GetRoleName(ChatMessageRoleType roleType)
    {
        return roleType switch
        {
            ChatMessageRoleType.User => "用户",
            ChatMessageRoleType.Assistant => "助手",
            ChatMessageRoleType.System => "系统",
            _ => "未知"
        };
    }
}
