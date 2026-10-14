using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AITokenUsageRecorderTests
{
    [TestMethod]
    public void Record_UpdatesLiveAggregationWithoutWaitingForPersistence()
    {
        var monitor = new AITokenMonitorService();
        var recorder = new AITokenUsageRecorder(
            monitor,
            new NoopScopeFactory(),
            NullLogger<AITokenUsageRecorder>.Instance);

        recorder.Record(new AITokenUsageRecord
        {
            ModelAlias = "PromptRange",
            Snapshot = new AITokenUsageSnapshot
            {
                InputTokens = 12,
                OutputTokens = 8
            },
            DurationMs = 25,
            Success = true,
            TenantId = 7,
            Source = "PromptRange.Tests"
        });

        var stats = monitor.GetLiveStats(1, tenantId: 7);

        Assert.AreEqual(1, stats.TotalCalls);
        Assert.AreEqual(12, stats.TotalInputTokens);
        Assert.AreEqual(8, stats.TotalOutputTokens);
        Assert.AreEqual(20, stats.TotalTokens);
        Assert.AreEqual("PromptRange", stats.ByModel.Single().ModelAlias);
    }

    private sealed class NoopScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new InvalidOperationException("Persistence should not be required by this test.");
        }
    }
}
