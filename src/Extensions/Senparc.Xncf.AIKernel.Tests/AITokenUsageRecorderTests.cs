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

    [TestMethod]
    public async Task StopAsync_WhenPersistenceIsStillRunningAndShutdownIsCanceled_ReturnsWithoutThrowing()
    {
        var recorder = new AITokenUsageRecorder(
            new AITokenMonitorService(),
            new SlowScopeFactory(),
            NullLogger<AITokenUsageRecorder>.Instance);
        await recorder.StartAsync(CancellationToken.None);
        recorder.Record(new AITokenUsageRecord
        {
            ModelAlias = "PromptRange",
            Snapshot = new AITokenUsageSnapshot { InputTokens = 1, OutputTokens = 1 },
            Success = true,
            Source = "Shutdown.Tests"
        });
        await Task.Delay(50);

        using var shutdown = new CancellationTokenSource();
        shutdown.Cancel();
        var startedAt = DateTime.UtcNow;
        await recorder.StopAsync(shutdown.Token);

        Assert.IsTrue(DateTime.UtcNow - startedAt < TimeSpan.FromSeconds(1.5));
    }

    private sealed class NoopScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new InvalidOperationException("Persistence should not be required by this test.");
        }
    }

    private sealed class SlowScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            throw new InvalidOperationException("Persistence should still be running when shutdown is requested.");
        }
    }
}
