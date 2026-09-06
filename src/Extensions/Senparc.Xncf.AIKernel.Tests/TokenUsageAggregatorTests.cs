using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class TokenUsageAggregatorTests
{
    private static AITokenUsage CreateRecord(string alias, long input, long output, long total, DateTime addTime)
    {
        return new AITokenUsage
        {
            ModelAlias = alias,
            InputTokens = input,
            OutputTokens = output,
            TotalTokens = total,
            AddTime = addTime
        };
    }

    [TestMethod]
    public void BuildModelUsageMap_NullRecords_ReturnsEmptyMap()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(null);

        Assert.AreEqual(0, map.Count);
    }

    [TestMethod]
    public void BuildModelUsageMap_EmptyRecords_ReturnsEmptyMap()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(Array.Empty<AITokenUsage>());

        Assert.AreEqual(0, map.Count);
    }

    [TestMethod]
    public void BuildModelUsageMap_GroupsByAlias_CaseInsensitive()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(new[]
        {
            CreateRecord("GPT-4o", 10, 5, 15, new DateTime(2026, 9, 1)),
            CreateRecord("gpt-4O", 1, 1, 2, new DateTime(2026, 9, 2))
        });

        Assert.AreEqual(1, map.Count);
        Assert.IsTrue(map.TryGetValue("gpt-4o", out var usage));
        Assert.AreEqual(2, usage.Calls);
        Assert.AreEqual(11, usage.InputTokens);
        Assert.AreEqual(6, usage.OutputTokens);
        Assert.AreEqual(17, usage.TotalTokens);
    }

    [TestMethod]
    public void BuildModelUsageMap_BlankAlias_GroupedAsUnknown()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(new[]
        {
            CreateRecord(null, 1, 1, 2, new DateTime(2026, 9, 1)),
            CreateRecord("   ", 2, 2, 4, new DateTime(2026, 9, 2))
        });

        Assert.AreEqual(1, map.Count);
        Assert.IsTrue(map.TryGetValue(TokenUsageAggregator.UnknownAlias, out var usage));
        Assert.AreEqual(2, usage.Calls);
        Assert.AreEqual(6, usage.TotalTokens);
    }

    [TestMethod]
    public void BuildModelUsageMap_TracksLastUseTime()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(new[]
        {
            CreateRecord("m", 1, 1, 2, new DateTime(2026, 9, 1)),
            CreateRecord("m", 1, 1, 2, new DateTime(2026, 9, 5)),
            CreateRecord("m", 1, 1, 2, new DateTime(2026, 9, 3))
        });

        var usage = map["m"];

        Assert.AreEqual(new DateTime(2026, 9, 5), usage.LastUseTime);
    }

    [TestMethod]
    public void BuildModelUsageMap_NullRecordsInList_AreIgnored()
    {
        var map = TokenUsageAggregator.BuildModelUsageMap(new AITokenUsage[]
        {
            null,
            CreateRecord("m", 3, 2, 5, new DateTime(2026, 9, 1)),
            null
        });

        Assert.AreEqual(1, map.Count);
        Assert.AreEqual(1, map["m"].Calls);
    }
}
