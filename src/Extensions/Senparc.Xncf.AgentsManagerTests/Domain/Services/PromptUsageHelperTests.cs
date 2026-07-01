/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptUsageHelperTests.cs
    文件功能描述：PromptUsageHelperTests 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

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
