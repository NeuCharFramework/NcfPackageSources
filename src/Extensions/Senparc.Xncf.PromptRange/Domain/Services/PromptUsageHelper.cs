/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptUsageHelper.cs
    File description: Related implementation.

    Created by: Senparc - 20260626

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using System;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public static class PromptUsageHelper
{
    public static PromptUsageInfo ResolveUsage(UsageDetails usageDetails)
    {
        var prompt = ClampToInt(usageDetails?.InputTokenCount ?? 0);
        var completion = ClampToInt(usageDetails?.OutputTokenCount ?? 0);
        var total = ClampToInt(usageDetails?.TotalTokenCount ?? 0);
        if (total <= 0)
        {
            total = prompt + completion;
        }

        return new PromptUsageInfo
        {
            PromptTokens = prompt,
            CompletionTokens = completion,
            TotalTokens = total
        };
    }

    private static int ClampToInt(long value)
    {
        if (value <= 0)
        {
            return 0;
        }

        return value > int.MaxValue ? int.MaxValue : (int)value;
    }
}

public sealed class PromptUsageInfo
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
