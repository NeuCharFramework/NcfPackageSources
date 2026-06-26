using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.PromptRange.Domain.Services;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class PromptUsageHelperTests
{
    [TestMethod]
    public void ResolveUsage_ShouldFallbackToPromptPlusCompletion()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 120,
            OutputTokenCount = 45,
            TotalTokenCount = 0
        };

        var result = PromptUsageHelper.ResolveUsage(usage);

        Assert.AreEqual(120, result.PromptTokens);
        Assert.AreEqual(45, result.CompletionTokens);
        Assert.AreEqual(165, result.TotalTokens);
    }

    [TestMethod]
    public void ResolveUsage_ShouldClampLargeValues()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = (long)int.MaxValue + 1000,
            OutputTokenCount = -1,
            TotalTokenCount = (long)int.MaxValue + 2000
        };

        var result = PromptUsageHelper.ResolveUsage(usage);

        Assert.AreEqual(int.MaxValue, result.PromptTokens);
        Assert.AreEqual(0, result.CompletionTokens);
        Assert.AreEqual(int.MaxValue, result.TotalTokens);
    }
}
