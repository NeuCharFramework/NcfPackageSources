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
