using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Tests
{
    [TestClass]
    public class AITokenMonitorServiceAsyncProgressTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

        private static AITokenProgressEvent CreateEvent(Guid runId, long outputTokens, AITokenProgressStatus status)
        {
            return new AITokenProgressEvent
            {
                RunId = runId,
                ModelAlias = "gpt-test",
                ModelId = "gpt-test",
                Status = status,
                InputTokens = 100,
                OutputTokens = outputTokens,
                TotalTokens = 100 + outputTokens,
                OutputPreview = outputTokens.ToString()
            };
        }

        [TestMethod]
        public void GetLatestProgress_UnknownRun_ReturnsNull()
        {
            var service = new AITokenMonitorService();

            Assert.IsNull(service.GetLatestProgress(Guid.NewGuid()));
        }

        [TestMethod]
        public void GetBufferedProgress_UnknownRun_ReturnsEmpty()
        {
            var service = new AITokenMonitorService();

            var buffered = service.GetBufferedProgress(Guid.NewGuid());

            Assert.IsNotNull(buffered);
            Assert.AreEqual(0, buffered.Count);
        }

        [TestMethod]
        public void PublishProgress_NullEvent_IsIgnored()
        {
            var service = new AITokenMonitorService();

            service.PublishProgress(null);

            Assert.IsNull(service.GetLatestProgress(Guid.NewGuid()));
        }

        [TestMethod]
        public void PublishProgress_SetsTimestamp_OnEvent()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            var before = DateTime.Now;

            service.PublishProgress(new AITokenProgressEvent
            {
                RunId = runId,
                Status = AITokenProgressStatus.Running,
                Timestamp = DateTime.MinValue
            });

            var latest = service.GetLatestProgress(runId);

            Assert.IsNotNull(latest);
            Assert.IsTrue(latest.Timestamp >= before);
        }

        [TestMethod]
        public void GetLatestProgress_ReturnsMostRecentEvent()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();

            service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 25, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 40, AITokenProgressStatus.Completed));

            var latest = service.GetLatestProgress(runId);

            Assert.IsNotNull(latest);
            Assert.AreEqual(40, latest.OutputTokens);
            Assert.AreEqual(AITokenProgressStatus.Completed, latest.Status);
        }

        [TestMethod]
        public void GetBufferedProgress_ReturnsAllEventsInOrder()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();

            service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 20, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 30, AITokenProgressStatus.Completed));

            var buffered = service.GetBufferedProgress(runId);

            CollectionAssert.AreEqual(new long[] { 10, 20, 30 }, buffered.Select(z => z.OutputTokens).ToList());
        }

        [TestMethod]
        public void GetBufferedProgress_DifferentRuns_AreIsolated()
        {
            var service = new AITokenMonitorService();
            var runA = Guid.NewGuid();
            var runB = Guid.NewGuid();

            service.PublishProgress(CreateEvent(runA, 1, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runB, 2, AITokenProgressStatus.Running));

            var bufferedA = service.GetBufferedProgress(runA);
            var bufferedB = service.GetBufferedProgress(runB);

            CollectionAssert.AreEqual(new long[] { 1 }, bufferedA.Select(z => z.OutputTokens).ToList());
            CollectionAssert.AreEqual(new long[] { 2 }, bufferedB.Select(z => z.OutputTokens).ToList());
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
        public async Task SubscribeAsync_ReplaysBuffer_AndStopsAfterComplete()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 20, AITokenProgressStatus.Running));
            service.PublishProgress(CreateEvent(runId, 30, AITokenProgressStatus.Completed));

            var received = new List<long>();
            var enumerator = service.SubscribeAsync(runId).GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TestTimeout))
                {
                    received.Add(enumerator.Current.OutputTokens);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            CollectionAssert.AreEqual(new long[] { 10, 20, 30 }, received);
        }

        [TestMethod]
        public async Task SubscribeAsync_LiveSubscription_ReceivesEventsPublishedAfterSubscribe()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();

            var enumerator = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
            try
            {
                var firstPending = enumerator.MoveNextAsync().AsTask();
                service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));
                Assert.IsTrue(await firstPending.WaitAsync(TestTimeout));
                Assert.AreEqual(10, enumerator.Current.OutputTokens);

                service.PublishProgress(CreateEvent(runId, 20, AITokenProgressStatus.Completed));
                Assert.IsTrue(await enumerator.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
                Assert.AreEqual(20, enumerator.Current.OutputTokens);

                Assert.IsFalse(await enumerator.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task SubscribeAsync_ReplayThenLive_MergesBufferAndLiveEvents()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));

            var received = new List<long>();
            var enumerator = service.SubscribeAsync(runId, replayBuffered: true).GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TestTimeout))
                {
                    received.Add(enumerator.Current.OutputTokens);
                    if (received.Count == 1)
                    {
                        service.PublishProgress(CreateEvent(runId, 20, AITokenProgressStatus.Running));
                        service.PublishProgress(CreateEvent(runId, 30, AITokenProgressStatus.Completed));
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            CollectionAssert.AreEqual(new long[] { 10, 20, 30 }, received);
        }

        [TestMethod]
        public async Task SubscribeAsync_CompletedRun_WithoutReplay_YieldsNothing()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Completed));

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

            var enumeratorA = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
            var enumeratorB = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
            try
            {
                var pendingA = enumeratorA.MoveNextAsync().AsTask();
                var pendingB = enumeratorB.MoveNextAsync().AsTask();
                service.PublishProgress(CreateEvent(runId, 10, AITokenProgressStatus.Running));
                Assert.IsTrue(await pendingA.WaitAsync(TestTimeout));
                Assert.IsTrue(await pendingB.WaitAsync(TestTimeout));
                Assert.AreEqual(10, enumeratorA.Current.OutputTokens);
                Assert.AreEqual(10, enumeratorB.Current.OutputTokens);

                service.PublishProgress(CreateEvent(runId, 20, AITokenProgressStatus.Completed));
                Assert.IsTrue(await enumeratorA.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
                Assert.IsTrue(await enumeratorB.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
                Assert.AreEqual(20, enumeratorA.Current.OutputTokens);
                Assert.AreEqual(20, enumeratorB.Current.OutputTokens);

                Assert.IsFalse(await enumeratorA.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
                Assert.IsFalse(await enumeratorB.MoveNextAsync().AsTask().WaitAsync(TestTimeout));
            }
            finally
            {
                await enumeratorA.DisposeAsync();
                await enumeratorB.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task SubscribeAsync_BurstPublish_DeliversAllEventsInOrder()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            const int eventCount = 500;

            var enumerator = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
            try
            {
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
                await publishTask.WaitAsync(TestTimeout);
                Assert.IsTrue(await firstPending.WaitAsync(TestTimeout));
                var received = new List<long> { enumerator.Current.OutputTokens };
                while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TestTimeout))
                {
                    received.Add(enumerator.Current.OutputTokens);
                }

                CollectionAssert.AreEqual(
                    Enumerable.Range(1, eventCount).Select(z => (long)z).ToList(),
                    received);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task SubscribeAsync_Cancellation_StopsEnumeration()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();

            using var cts = new CancellationTokenSource();
            var enumerator = service.SubscribeAsync(runId, replayBuffered: false, cts.Token).GetAsyncEnumerator();
            try
            {
                var pending = enumerator.MoveNextAsync().AsTask();
                await Task.Delay(100);
                cts.Cancel();

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    async () => await pending);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task PublishProgress_ConcurrentPublishers_NoEventsLost()
        {
            var service = new AITokenMonitorService();
            var runId = Guid.NewGuid();
            const int publisherCount = 4;
            const int eventsPerPublisher = 100;

            var enumerator = service.SubscribeAsync(runId, replayBuffered: false).GetAsyncEnumerator();
            try
            {
                var firstPending = enumerator.MoveNextAsync().AsTask();

                var tasks = Enumerable.Range(0, publisherCount)
                    .Select(publisher => Task.Run(() =>
                    {
                        for (int i = 0; i < eventsPerPublisher; i++)
                        {
                            service.PublishProgress(CreateEvent(runId, i, AITokenProgressStatus.Running));
                        }
                    }));
                await Task.WhenAll(tasks);
                service.PublishProgress(CreateEvent(runId, 0, AITokenProgressStatus.Completed));

                Assert.IsTrue(await firstPending.WaitAsync(TimeSpan.FromSeconds(30)));
                var received = 1;
                while (await enumerator.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30)))
                {
                    received++;
                }

                Assert.AreEqual(publisherCount * eventsPerPublisher + 1, received);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }
    }
}
