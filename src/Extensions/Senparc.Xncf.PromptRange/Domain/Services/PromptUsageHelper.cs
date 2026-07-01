/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptUsageHelper.cs
    文件功能描述：PromptUsageHelper 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

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
