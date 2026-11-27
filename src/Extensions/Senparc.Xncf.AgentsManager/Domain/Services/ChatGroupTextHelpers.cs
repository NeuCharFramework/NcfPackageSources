/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupTextHelpers.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Creation description: Related implementation.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Converts AgentKernel responses and streaming content to normalized text and
/// applies bounded chunking and user-facing failure messages for ChatGroup execution.
/// </summary>
internal static class ChatGroupTextHelpers
{
    /// <summary>
    /// Gets the default maximum stream chunk length.
    /// </summary>
    private const int DefaultStreamChunkLength = 24;

    /// <summary>Clamps a long token/count value to the non-negative Int32 range.</summary>
    /// <param name="value">The value to clamp.</param>
    /// <returns>The clamped integer.</returns>
    public static int ClampToInt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > int.MaxValue ? int.MaxValue : (int)value;
    }


    /// <summary>Extracts readable text from an AgentKernel response.</summary>
    /// <param name="response">The response to inspect.</param>
    /// <returns>Combined response text, or an empty string when no text is available.</returns>
    public static string ExtractAgentResponseText(AgentResponse response)
    {
        if (response == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            return response.Text;
        }

        if (response.Messages == null || response.Messages.Count == 0)
        {
            return string.Empty;
        }

        var messageTexts = response.Messages
            .Select(ExtractChatMessageText)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        return messageTexts.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, messageTexts);
    }


    /// <summary>Extracts text content from one chat message.</summary>
    /// <param name="chatMessage">The message to inspect.</param>
    /// <returns>The message text, or an empty string when no content exists.</returns>
    public static string ExtractChatMessageText(ChatMessage chatMessage)
    {
        if (chatMessage == null)
        {
            return string.Empty;
        }

        var textSegments = chatMessage.Contents?
            .OfType<TextContent>()
            .Select(z => z.Text)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        if (textSegments?.Count > 0)
        {
            return string.Join(Environment.NewLine, textSegments);
        }

        return chatMessage.ToString() ?? string.Empty;
    }


    /// <summary>Extracts text content from one streaming response update.</summary>
    /// <param name="update">The response update to inspect.</param>
    /// <returns>The update text, or an empty string when no text exists.</returns>
    public static string ExtractAgentResponseUpdateText(AgentResponseUpdate update)
    {
        if (update == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(update.Text))
        {
            return update.Text;
        }

        var textSegments = update.Contents?
            .OfType<TextContent>()
            .Select(z => z.Text)
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .ToList();

        if (textSegments?.Count > 0)
        {
            return string.Join(Environment.NewLine, textSegments);
        }

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        return string.Empty;
    }


    /// <summary>Determines whether text represents a supported user exit command.</summary>
    /// <param name="text">The candidate exit text.</param>
    /// <returns><see langword="true"/> for supported English or Chinese exit commands.</returns>
    public static bool IsExitSignal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        normalized = normalized.TrimEnd('.', '!', '?', ';', ':', '。', '！', '？', '；', '：', ']', '>');
        normalized = normalized.TrimStart('[', '<');

        return normalized.Equals("exit", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("结束", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("退出", StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>Splits response text at punctuation/newlines and a maximum chunk length.</summary>
    /// <param name="text">The text to split.</param>
    /// <param name="maxChunkLength">The maximum chunk length.</param>
    /// <returns>The ordered stream chunks.</returns>
    public static IEnumerable<string> SplitStreamText(string text, int maxChunkLength = DefaultStreamChunkLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var buffer = new StringBuilder();
        foreach (var ch in text)
        {
            buffer.Append(ch);
            if (ShouldBreakStreamChunk(ch, buffer.Length, maxChunkLength))
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }


    /// <summary>Determines whether a streaming chunk should be emitted.</summary>
    /// <param name="ch">The most recently appended character.</param>
    /// <param name="currentLength">The current chunk length.</param>
    /// <param name="maxChunkLength">The configured maximum length.</param>
    /// <returns><see langword="true"/> when punctuation or length requires a break.</returns>
    public static bool ShouldBreakStreamChunk(char ch, int currentLength, int maxChunkLength)
    {
        if (currentLength >= maxChunkLength)
        {
            return true;
        }

        return ch is '\n' or '。' or '！' or '？' or '!' or '?' or ';' or '；' or '，' or ',';
    }


    /// <summary>Builds a bounded, user-facing message for an agent execution failure.</summary>
    /// <param name="ex">The failure exception.</param>
    /// <returns>A normalized actionable failure message.</returns>
    public static string BuildAgentExecutionFailureMessage(Exception ex)
    {
        var raw = ex?.Message ?? "未知错误";
        var normalized = raw
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (normalized.Length > 240)
        {
            normalized = normalized[..240];
        }

        if (normalized.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Forbidden", StringComparison.OrdinalIgnoreCase))
        {
            return "系统提示：AI 服务返回 403（Forbidden），当前任务无法继续。请检查模型权限、API Key 或所选模型可用性。";
        }

        if (normalized.Contains("Status: 401", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "系统提示：AI 服务认证失败（401），请检查 API Key / Endpoint 配置。";
        }

        if (normalized.Contains("model is required", StringComparison.OrdinalIgnoreCase))
        {
            return "系统提示：Ollama 未配置模型名称。请在 AIKernel 模型配置中填写 ModelId，" +
                   "或在启动任务时选择有效模型。";
        }

        return $"系统提示：AI 服务调用失败，任务已中断。原因：{normalized}";
    }


}
