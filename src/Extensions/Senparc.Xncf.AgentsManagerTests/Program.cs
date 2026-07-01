/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Program.cs
    文件功能描述：Program 相关实现
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Senparc.Xncf.AgentsManager.Domain.Services.Tests;
using System;

namespace Senparc.Xncf.AgentsManagerTests;

public static class Program
{
    public static int Main(string[] args)
    {
        var tests = new UsageAnalyticsTests();

        try
        {
            tests.EncodeDecodeMessageRemark_ShouldKeepUsageData();
            tests.ChatTaskDto_ShouldParseAggregateUsageFromAdminRemark();

            var promptUsageTests = new PromptUsageHelperTests();
            promptUsageTests.ResolveUsage_ShouldFallbackToPromptPlusCompletion();
            promptUsageTests.ResolveUsage_ShouldClampLargeValues();

            Console.WriteLine("UsageAnalyticsTests passed.");
            Console.WriteLine("PromptUsageHelperTests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UsageAnalyticsTests failed: {ex}");
            return 1;
        }
    }
}
