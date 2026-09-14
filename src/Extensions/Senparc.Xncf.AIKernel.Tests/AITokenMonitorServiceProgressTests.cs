namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AITokenMonitorServiceProgressTests
{
    private static AITokenProgressEvent CreateEvent(Guid runId, long outputTokens, AITokenProgressStatus status)
    {
        return new AITokenProgressEvent
        {
            RunId = runId,
            ModelAlias = "test-model",
            ModelId = "test-model-id",
            Status = status,
            InputTokens = 10,
            OutputTokens = outputTokens,
            TotalTokens = 10 + outputTokens,
            OutputPreview = $"preview-{outputTokens}"
        };
    }

    [TestMethod]
    public void GetLatestProgress_UnknownRun_ReturnsNull()
    {
        Assert.IsNull(new AITokenMonitorService().GetLatestProgress(Guid.NewGuid()));
    }

    [TestMethod]
    public void GetBufferedProgress_UnknownRun_ReturnsEmpty()
    {
        var buffered = new AITokenMonitorService().GetBufferedProgress(Guid.NewGuid());

        Assert.IsNotNull(buffered);
        Assert.AreEqual(0, buffered.Count);
    }

    [TestMethod]
    public void PublishProgress_Null_IsIgnored()
    {
        var service = new AITokenMonitorService();

        service.PublishProgress(null);

        Assert.IsNull(service.GetLatestProgress(Guid.NewGuid()));
    }

    [TestMethod]
    public void PublishProgress_UpdatesTimestamp()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();
        var before = DateTime.Now;

        service.PublishProgress(new AITokenProgressEvent { RunId = runId, Timestamp = DateTime.MinValue });

        var latest = service.GetLatestProgress(runId);

        Assert.IsNotNull(latest);
        Assert.IsTrue(latest.Timestamp >= before);
    }

    [TestMethod]
    public void GetLatestProgress_ReturnsMostRecentEvent()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();

        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 3, AITokenProgressStatus.Completed));

        var latest = service.GetLatestProgress(runId);

        Assert.IsNotNull(latest);
        Assert.AreEqual(3, latest.OutputTokens);
        Assert.AreEqual(AITokenProgressStatus.Completed, latest.Status);
    }

    [TestMethod]
    public void GetBufferedProgress_ReturnsAllEventsInOrder()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();

        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 3, AITokenProgressStatus.Completed));

        var buffered = service.GetBufferedProgress(runId);

        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, buffered.Select(z => z.OutputTokens).ToList());
    }

    [TestMethod]
    public async Task SubscribeAsync_EmptyRunId_YieldsNothing()
    {
        var service = new AITokenMonitorService();
        var count = 0;

        await foreach (var _ in service.SubscribeAsync(Guid.Empty))
        {
            count++;
        }

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task SubscribeAsync_ReplaysBufferedEvents_StopsAtComplete()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();
        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runId, 3, AITokenProgressStatus.Completed));

        var received = new List<AITokenProgressEvent>();
        await foreach (var evt in service.SubscribeAsync(runId))
        {
            received.Add(evt);
        }

        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, received.Select(z => z.OutputTokens).ToList());
        Assert.IsTrue(received.All(z => z.RunId == runId));
    }

    [TestMethod]
    public async Task SubscribeAsync_LiveSubscription_ReceivesEventsAfterSubscribe()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();

        var enumerator = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
        var firstPending = enumerator.MoveNextAsync().AsTask();
        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));
        Assert.IsTrue(await firstPending);
        Assert.AreEqual(1, enumerator.Current.OutputTokens);

        service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Completed));
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(2, enumerator.Current.OutputTokens);

        Assert.IsFalse(await enumerator.MoveNextAsync());
        await enumerator.DisposeAsync();
    }

    [TestMethod]
    public async Task SubscribeAsync_MixedReplayAndLive_ContinuesIntoLiveEvents()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();
        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));

        var received = new List<long>();
        await foreach (var evt in service.SubscribeAsync(runId, replayBuffered: true))
        {
            received.Add(evt.OutputTokens);
            if (received.Count == 1)
            {
                service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Running));
                service.PublishProgress(CreateEvent(runId, 3, AITokenProgressStatus.Completed));
            }
        }

        CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, received.ToList());
    }

    [TestMethod]
    public async Task SubscribeAsync_CompletedRun_WithoutReplay_YieldsNothing()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();
        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Completed));

        var count = 0;
        await foreach (var _ in service.SubscribeAsync(runId, replayBuffered: false))
        {
            count++;
        }

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task SubscribeAsync_MultipleSubscribers_BothReceiveFullSequence()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();

        var subA = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
        var subB = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
        var pendingA = subA.MoveNextAsync().AsTask();
        var pendingB = subB.MoveNextAsync().AsTask();

        service.PublishProgress(CreateEvent(runId, 1, AITokenProgressStatus.Running));
        Assert.IsTrue(await pendingA);
        Assert.IsTrue(await pendingB);
        Assert.AreEqual(1, subA.Current.OutputTokens);
        Assert.AreEqual(1, subB.Current.OutputTokens);

        service.PublishProgress(CreateEvent(runId, 2, AITokenProgressStatus.Completed));
        Assert.IsTrue(await subA.MoveNextAsync());
        Assert.IsTrue(await subB.MoveNextAsync());
        Assert.AreEqual(2, subA.Current.OutputTokens);
        Assert.AreEqual(2, subB.Current.OutputTokens);

        Assert.IsFalse(await subA.MoveNextAsync());
        Assert.IsFalse(await subB.MoveNextAsync());
        await subA.DisposeAsync();
        await subB.DisposeAsync();
    }

    [TestMethod]
    public async Task SubscribeAsync_BurstPublish_DeliversAllEventsWithoutLoss()
    {
        var service = new AITokenMonitorService();
        var runId = Guid.NewGuid();
        const int eventCount = 500;

        var enumerator = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
        var firstPending = enumerator.MoveNextAsync().AsTask();

        var publishTask = Task.Run(() =>
        {
            for (int i = 1; i <= eventCount; i++)
            {
                service.PublishProgress(CreateEvent(
                    runId,
                    i,
                    i == eventCount ? AITokenProgressStatus.Completed : AITokenProgressStatus.Running));
            }
        });
        await publishTask;
        Assert.IsTrue(await firstPending);

        var received = new List<long> { enumerator.Current.OutputTokens };
        while (await enumerator.MoveNextAsync())
        {
            received.Add(enumerator.Current.OutputTokens);
        }
        await enumerator.DisposeAsync();

        Assert.AreEqual(eventCount, received.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, eventCount).Select(z => (long)z).ToList(), received);
    }

    [TestMethod]
    public void GetBufferedProgress_RunsAreIsolatedByRunId()
    {
        var service = new AITokenMonitorService();
        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();
        service.PublishProgress(CreateEvent(runA, 1, AITokenProgressStatus.Running));
        service.PublishProgress(CreateEvent(runB, 9, AITokenProgressStatus.Completed));

        Assert.AreEqual(1, service.GetBufferedProgress(runA).Count);
        Assert.AreEqual(9, service.GetLatestProgress(runB).OutputTokens);
    }
}
