/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：UsageAnalyticsTests.cs
    文件功能描述：UsageAnalyticsTests 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class UsageAnalyticsTests
{
    [TestMethod]
    public void EncodeDecodeMessageRemark_ShouldKeepUsageData()
    {
        var snapshot = new ChatUsageSnapshot
        {
            PromptTokens = 120,
            CompletionTokens = 80,
            TotalTokens = 200,
            ResponseMilliseconds = 1450,
            RoundIndex = 3,
            ResponseId = "resp-abc-123"
        };

        var encoded = ChatUsageRemarkCodec.EncodeMessage(snapshot);
        Assert.IsTrue(ChatUsageRemarkCodec.TryDecodeMessage(encoded, out var decoded));
        Assert.AreEqual(snapshot.PromptTokens, decoded.PromptTokens);
        Assert.AreEqual(snapshot.CompletionTokens, decoded.CompletionTokens);
        Assert.AreEqual(snapshot.TotalTokens, decoded.TotalTokens);
        Assert.AreEqual(snapshot.ResponseMilliseconds, decoded.ResponseMilliseconds);
        Assert.AreEqual(snapshot.RoundIndex, decoded.RoundIndex);
        Assert.AreEqual(snapshot.ResponseId, decoded.ResponseId);
    }

    [TestMethod]
    public void ChatTaskDto_ShouldParseAggregateUsageFromAdminRemark()
    {
        var chatTask = new ChatTask(new ChatTaskDto(
            name: "UsageTask",
            chatGroupId: 1,
            aiModelId: 1,
            status: ChatTask_Status.Chatting,
            promptCommand: "test",
            description: "desc",
            isPersonality: true,
            hookPlatform: HookPlatform.None,
            hookPlatformParameter: string.Empty,
            score: false,
            startTime: DateTime.Now.AddMinutes(-2),
            endTime: DateTime.Now,
            resultComment: null));

        var aggregate = new ChatTaskUsageAggregate();
        aggregate.Merge(new ChatUsageSnapshot
        {
            PromptTokens = 100,
            CompletionTokens = 40,
            TotalTokens = 140,
            ResponseMilliseconds = 1000
        });
        aggregate.Merge(new ChatUsageSnapshot
        {
            PromptTokens = 60,
            CompletionTokens = 30,
            TotalTokens = 90,
            ResponseMilliseconds = 1500
        });

        chatTask.AdminRemark = ChatUsageRemarkCodec.EncodeAggregate(aggregate);
        var dto = new ChatTaskDto(chatTask);

        Assert.AreEqual(2, dto.TotalRounds);
        Assert.AreEqual(160, dto.TotalPromptTokens);
        Assert.AreEqual(70, dto.TotalCompletionTokens);
        Assert.AreEqual(230, dto.TotalTokens);
        Assert.AreEqual(1500, dto.MaxResponseMilliseconds);
        Assert.AreEqual(1250, dto.AverageResponseMilliseconds);
    }
}
