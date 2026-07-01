/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatUsageRemarkCodec.cs
    文件功能描述：ChatUsageRemarkCodec 数据模型定义
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Senparc.Xncf.AgentsManager.Domain.Models.Usage;

/// <summary>
/// Compact codec for storing usage statistics in EntityBase.AdminRemark.
/// </summary>
public static class ChatUsageRemarkCodec
{
    private const string MessagePrefix = "u1";
    private const string AggregatePrefix = "a1";

    public static string EncodeMessage(ChatUsageSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return null;
        }

        var responseId = string.IsNullOrWhiteSpace(snapshot.ResponseId)
            ? "-"
            : snapshot.ResponseId.Trim();

        if (responseId.Length > 32)
        {
            responseId = responseId.Substring(0, 32);
        }

        var safeRound = Math.Max(0, snapshot.RoundIndex);
        var safePrompt = Math.Max(0, snapshot.PromptTokens);
        var safeCompletion = Math.Max(0, snapshot.CompletionTokens);
        var safeTotal = Math.Max(0, snapshot.TotalTokens);
        var safeResponseMs = Math.Max(0, snapshot.ResponseMilliseconds);

        return $"{MessagePrefix}|pt:{safePrompt}|ct:{safeCompletion}|tt:{safeTotal}|ms:{safeResponseMs}|r:{safeRound}|id:{responseId}";
    }

    public static bool TryDecodeMessage(string remark, out ChatUsageSnapshot snapshot)
    {
        snapshot = null;
        if (!TryDecodeInternal(remark, MessagePrefix, out var map))
        {
            return false;
        }

        snapshot = new ChatUsageSnapshot
        {
            PromptTokens = GetInt(map, "pt"),
            CompletionTokens = GetInt(map, "ct"),
            TotalTokens = GetInt(map, "tt"),
            ResponseMilliseconds = GetInt(map, "ms"),
            RoundIndex = GetInt(map, "r"),
            ResponseId = GetString(map, "id")
        };

        if (snapshot.TotalTokens == 0)
        {
            snapshot.TotalTokens = snapshot.PromptTokens + snapshot.CompletionTokens;
        }

        return true;
    }

    public static string EncodeAggregate(ChatTaskUsageAggregate aggregate)
    {
        if (aggregate == null)
        {
            return null;
        }

        var messageCount = Math.Max(0, aggregate.MessageCount);
        var prompt = Math.Max(0, aggregate.PromptTokens);
        var completion = Math.Max(0, aggregate.CompletionTokens);
        var total = Math.Max(0, aggregate.TotalTokens);
        var totalResponseMs = Math.Max(0, aggregate.TotalResponseMilliseconds);
        var maxResponseMs = Math.Max(0, aggregate.MaxResponseMilliseconds);

        return $"{AggregatePrefix}|m:{messageCount}|pt:{prompt}|ct:{completion}|tt:{total}|sr:{totalResponseMs}|mx:{maxResponseMs}";
    }

    public static bool TryDecodeAggregate(string remark, out ChatTaskUsageAggregate aggregate)
    {
        aggregate = null;
        if (!TryDecodeInternal(remark, AggregatePrefix, out var map))
        {
            return false;
        }

        aggregate = new ChatTaskUsageAggregate
        {
            MessageCount = GetInt(map, "m"),
            PromptTokens = GetInt(map, "pt"),
            CompletionTokens = GetInt(map, "ct"),
            TotalTokens = GetInt(map, "tt"),
            TotalResponseMilliseconds = GetInt(map, "sr"),
            MaxResponseMilliseconds = GetInt(map, "mx")
        };

        if (aggregate.TotalTokens == 0)
        {
            aggregate.TotalTokens = aggregate.PromptTokens + aggregate.CompletionTokens;
        }

        return true;
    }

    public static ChatTaskUsageAggregate DecodeAggregateOrDefault(string remark)
    {
        return TryDecodeAggregate(remark, out var aggregate)
            ? aggregate
            : new ChatTaskUsageAggregate();
    }

    private static bool TryDecodeInternal(string remark, string expectedPrefix, out Dictionary<string, string> map)
    {
        map = null;
        if (string.IsNullOrWhiteSpace(remark))
        {
            return false;
        }

        var segments = remark.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || !string.Equals(segments[0], expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        map = segments
            .Skip(1)
            .Select(z =>
            {
                var index = z.IndexOf(':');
                if (index <= 0 || index >= z.Length - 1)
                {
                    return (Key: string.Empty, Value: string.Empty);
                }

                return (Key: z.Substring(0, index), Value: z[(index + 1)..]);
            })
            .Where(z => !string.IsNullOrWhiteSpace(z.Key))
            .ToDictionary(z => z.Key, z => z.Value, StringComparer.OrdinalIgnoreCase);

        return map.Count > 0;
    }

    private static int GetInt(Dictionary<string, string> map, string key)
    {
        if (map.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        return 0;
    }

    private static string GetString(Dictionary<string, string> map, string key)
    {
        return map.TryGetValue(key, out var value) && value != "-"
            ? value
            : string.Empty;
    }
}

public sealed class ChatUsageSnapshot
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ResponseMilliseconds { get; set; }
    public int RoundIndex { get; set; }
    public string ResponseId { get; set; }
}

public sealed class ChatTaskUsageAggregate
{
    public int MessageCount { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int TotalResponseMilliseconds { get; set; }
    public int MaxResponseMilliseconds { get; set; }

    public double AverageResponseMilliseconds =>
        MessageCount <= 0 ? 0 : (double)TotalResponseMilliseconds / MessageCount;

    public void Merge(ChatUsageSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        MessageCount += 1;
        PromptTokens += Math.Max(0, snapshot.PromptTokens);
        CompletionTokens += Math.Max(0, snapshot.CompletionTokens);
        TotalTokens += Math.Max(0, snapshot.TotalTokens > 0 ? snapshot.TotalTokens : snapshot.PromptTokens + snapshot.CompletionTokens);
        var responseMs = Math.Max(0, snapshot.ResponseMilliseconds);
        TotalResponseMilliseconds += responseMs;
        if (responseMs > MaxResponseMilliseconds)
        {
            MaxResponseMilliseconds = responseMs;
        }
    }
}
