/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatGroupTextHelpers.cs
    文件功能描述：ChatGroup 执行的文本/流式辅助逻辑（从 ChatGroupService 抽取）
    
    创建标识：Senparc - 20260927
    创建描述：拆分 ChatGroupService，集中管理响应文本提取、流式分块与退出信号识别

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
/// ChatGroup 执行的文本/流式辅助逻辑（从 ChatGroupService 抽取的纯函数集合）。
/// </summary>
internal static class ChatGroupTextHelpers
{
    /// <summary>
    /// 流式文本默认分块长度（字符数）。
    /// </summary>
    private const int DefaultStreamChunkLength = 24;

    public static int ClampToInt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > int.MaxValue ? int.MaxValue : (int)value;
    }


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

        // 只认真实文本内容，避免把 ToString() 结果误判为“已流式输出”，
        // 从而错过后续 synthetic chunk 回退。
        return string.Empty;
    }


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


    public static bool ShouldBreakStreamChunk(char ch, int currentLength, int maxChunkLength)
    {
        if (currentLength >= maxChunkLength)
        {
            return true;
        }

        return ch is '\n' or '。' or '！' or '？' or '!' or '?' or ';' or '；' or '，' or ',';
    }


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
