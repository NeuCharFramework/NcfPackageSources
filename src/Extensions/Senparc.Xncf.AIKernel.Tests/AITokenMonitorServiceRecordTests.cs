namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AITokenMonitorServiceRecordTests
{
    [TestMethod]
    public void GetLiveStats_EmptyService_ReturnsZero()
    {
        var stats = new AITokenMonitorService().GetLiveStats();

        Assert.AreEqual(0, stats.TotalCalls);
        Assert.AreEqual(0, stats.SuccessCount);
        Assert.AreEqual(0, stats.ErrorCount);
        Assert.AreEqual(0, stats.TotalInputTokens);
        Assert.AreEqual(0, stats.TotalOutputTokens);
        Assert.AreEqual(0, stats.TotalTokens);
        Assert.AreEqual(0, stats.AverageDurationMs);
        Assert.AreEqual(0, stats.ByModel.Count);
        Assert.AreEqual(7, stats.Daily.Count);
        Assert.IsTrue(stats.Daily.All(z => z.Calls == 0 && z.TotalTokens == 0));
    }

    [TestMethod]
    public void Record_AccumulatesTotalsAndAverageDuration()
    {
        var service = new AITokenMonitorService();
        service.Record("model-a", new AITokenUsageSnapshot { InputTokens = 10, OutputTokens = 5 }, 100, success: true);
        service.Record("model-b", new AITokenUsageSnapshot { InputTokens = 20, OutputTokens = 15 }, 300, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(2, stats.TotalCalls);
        Assert.AreEqual(30, stats.TotalInputTokens);
        Assert.AreEqual(20, stats.TotalOutputTokens);
        Assert.AreEqual(50, stats.TotalTokens);
        Assert.AreEqual(200, stats.AverageDurationMs);
    }

    [TestMethod]
    public void Record_CountsSuccessAndError()
    {
        var service = new AITokenMonitorService();
        service.Record("m", null, 10, success: true);
        service.Record("m", null, 10, success: false);
        service.Record("m", null, 10, success: false);

        var stats = service.GetLiveStats();

        Assert.AreEqual(1, stats.SuccessCount);
        Assert.AreEqual(2, stats.ErrorCount);
        Assert.AreEqual(3, stats.TotalCalls);
    }

    [TestMethod]
    public void Record_NullSnapshot_RecordsZeroTokens()
    {
        var service = new AITokenMonitorService();
        service.Record("m", null, 10, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(1, stats.TotalCalls);
        Assert.AreEqual(0, stats.TotalTokens);
    }

    [TestMethod]
    public void Record_NegativeSnapshotValues_AreClampedToZero()
    {
        var service = new AITokenMonitorService();
        service.Record("m", new AITokenUsageSnapshot { InputTokens = -10, OutputTokens = -5 }, -50, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(0, stats.TotalInputTokens);
        Assert.AreEqual(0, stats.TotalOutputTokens);
        Assert.AreEqual(0, stats.TotalTokens);
        Assert.AreEqual(0, stats.AverageDurationMs);
    }

    [TestMethod]
    public void Record_TotalFallback_UsesInputPlusOutput()
    {
        var service = new AITokenMonitorService();
        service.Record("m", new AITokenUsageSnapshot { InputTokens = 10, OutputTokens = 5 }, 10, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(15, stats.TotalTokens);
    }

    [TestMethod]
    public void Record_ModelAggregation_IsCaseInsensitive()
    {
        var service = new AITokenMonitorService();
        service.Record("GPT-4o", new AITokenUsageSnapshot { InputTokens = 10, OutputTokens = 5 }, 10, success: true);
        service.Record("gpt-4O", new AITokenUsageSnapshot { InputTokens = 1, OutputTokens = 1 }, 10, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(1, stats.ByModel.Count);
        Assert.AreEqual(2, stats.ByModel[0].Calls);
        Assert.AreEqual(17, stats.ByModel[0].TotalTokens);
    }

    [TestMethod]
    public void Record_NullOrBlankModelAlias_GroupedAsUnknown()
    {
        var service = new AITokenMonitorService();
        service.Record(null, new AITokenUsageSnapshot { InputTokens = 1, OutputTokens = 1 }, 10, success: true);
        service.Record("   ", new AITokenUsageSnapshot { InputTokens = 2, OutputTokens = 2 }, 10, success: true);

        var stats = service.GetLiveStats();

        Assert.AreEqual(1, stats.ByModel.Count);
        Assert.AreEqual("(unknown)", stats.ByModel[0].ModelAlias);
        Assert.AreEqual(2, stats.ByModel[0].Calls);
    }

    [TestMethod]
    public void GetLiveStats_ByModel_SortedByTotalTokensDescending()
    {
        var service = new AITokenMonitorService();
        service.Record("small", new AITokenUsageSnapshot { InputTokens = 1, OutputTokens = 1 }, 10, success: true);
        service.Record("big", new AITokenUsageSnapshot { InputTokens = 100, OutputTokens = 100 }, 10, success: true);
        service.Record("mid", new AITokenUsageSnapshot { InputTokens = 10, OutputTokens = 10 }, 10, success: true);

        var stats = service.GetLiveStats();

        CollectionAssert.AreEqual(new[] { "big", "mid", "small" }, stats.ByModel.Select(z => z.ModelAlias).ToList());
    }

    [TestMethod]
    public void GetLiveStats_Daily_CoversRequestedDaysInAscendingOrder()
    {
        var service = new AITokenMonitorService();
        service.Record("m", new AITokenUsageSnapshot { InputTokens = 3, OutputTokens = 2 }, 10, success: true);

        var stats = service.GetLiveStats(dailyDays: 3);
        var today = DateTime.Now.Date;

        Assert.AreEqual(3, stats.Daily.Count);
        Assert.AreEqual(today.AddDays(-2), stats.Daily[0].Date);
        Assert.AreEqual(today, stats.Daily[2].Date);
        Assert.AreEqual(today.ToString("yyyy-MM-dd"), stats.Daily[2].DateText);
        Assert.AreEqual(1, stats.Daily[2].Calls);
        Assert.AreEqual(5, stats.Daily[2].TotalTokens);
        Assert.AreEqual(0, stats.Daily[0].Calls);
    }

    [TestMethod]
    public async Task Record_ConcurrentCalls_DoNotLoseData()
    {
        var service = new AITokenMonitorService();
        const int threadCount = 8;
        const int perThread = 250;

        var tasks = Enumerable.Range(0, threadCount)
            .Select(thread => Task.Run(() =>
            {
                for (int i = 0; i < perThread; i++)
                {
                    service.Record("m", new AITokenUsageSnapshot { InputTokens = 2, OutputTokens = 1 }, 3, success: true);
                }
            }));
        await Task.WhenAll(tasks);

        var stats = service.GetLiveStats();

        Assert.AreEqual(threadCount * perThread, stats.TotalCalls);
        Assert.AreEqual(threadCount * perThread * 2, stats.TotalInputTokens);
        Assert.AreEqual(threadCount * perThread * 3, stats.TotalTokens);
        Assert.AreEqual(1, stats.ByModel.Count);
        Assert.AreEqual(threadCount * perThread, stats.ByModel[0].Calls);
    }
}
