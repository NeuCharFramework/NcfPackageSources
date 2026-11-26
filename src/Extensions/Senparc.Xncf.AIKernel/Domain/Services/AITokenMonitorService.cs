/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenMonitorService.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// Aggregates per-tenant AI token usage and exposes bounded live progress streams.
    /// </summary>
    public sealed class AITokenMonitorService
    {
        #region Legacy section

        private sealed class TenantAgg
        {
            public long TotalCalls;
            public long SuccessCount;
            public long ErrorCount;
            public long TotalInputTokens;
            public long TotalOutputTokens;
            public long TotalTokens;
            public long TotalDurationMs;
            public readonly ConcurrentDictionary<string, ModelAgg> ModelAggs = new(StringComparer.OrdinalIgnoreCase);
            public readonly ConcurrentDictionary<string, DailyAgg> DailyAggs = new(StringComparer.Ordinal);
        }

        private readonly ConcurrentDictionary<int, TenantAgg> _tenantAggs = new();

        private sealed class ModelAgg
        {
            public long Calls;
            public long InputTokens;
            public long OutputTokens;
            public long TotalTokens;
            public readonly object Sync = new();
        }

        private sealed class DailyAgg
        {
            public long Calls;
            public long InputTokens;
            public long OutputTokens;
            public long TotalTokens;
            public readonly object Sync = new();
        }

        #endregion

        #region Legacy section

        private sealed class RunState
        {
            public int TenantId;
            public readonly object Sync = new();
            public readonly List<AITokenProgressEvent> Buffer = new();
            public readonly ConcurrentDictionary<Guid, Channel<AITokenProgressEvent>> Subscribers = new();
            public bool IsComplete;
        }

        private readonly ConcurrentDictionary<Guid, RunState> _runs = new();

        /// <summary>
        /// Limits retained completed run states so in-memory progress tracking remains bounded.
        /// </summary>
        private const int MaxCompletedRuns = 256;

        #endregion

        /// <summary>
        /// Records normalized token counts, call status, duration, model totals, and daily totals.
        /// </summary>
        /// <param name="modelAlias">The model alias used for model-level aggregation.</param>
        /// <param name="snapshot">The token usage snapshot; negative or missing values are normalized to zero.</param>
        /// <param name="durationMs">The elapsed call duration in milliseconds.</param>
        /// <param name="success">Whether the call completed successfully.</param>
        /// <param name="tenantId">The tenant bucket receiving the usage.</param>
        public void Record(string modelAlias, AITokenUsageSnapshot snapshot, int durationMs, bool success, int tenantId = 0)
        {
            var tenant = _tenantAggs.GetOrAdd(tenantId, _ => new TenantAgg());
            var input = Math.Max(0, snapshot?.InputTokens ?? 0);
            var output = Math.Max(0, snapshot?.OutputTokens ?? 0);
            var total = Math.Max(0, snapshot?.TotalTokens ?? 0);
            if (total <= 0)
            {
                total = input + output;
            }
            var ms = Math.Max(0, durationMs);

            Interlocked.Increment(ref tenant.TotalCalls);
            Interlocked.Add(ref tenant.TotalInputTokens, input);
            Interlocked.Add(ref tenant.TotalOutputTokens, output);
            Interlocked.Add(ref tenant.TotalTokens, total);
            Interlocked.Add(ref tenant.TotalDurationMs, ms);
            if (success)
            {
                Interlocked.Increment(ref tenant.SuccessCount);
            }
            else
            {
                Interlocked.Increment(ref tenant.ErrorCount);
            }

            var key = string.IsNullOrWhiteSpace(modelAlias) ? "(unknown)" : modelAlias;
            var modelAgg = tenant.ModelAggs.GetOrAdd(key, _ => new ModelAgg());
            lock (modelAgg.Sync)
            {
                modelAgg.Calls += 1;
                modelAgg.InputTokens += input;
                modelAgg.OutputTokens += output;
                modelAgg.TotalTokens += total;
            }

            var dayKey = DateTime.Now.ToString("yyyy-MM-dd");
            var dailyAgg = tenant.DailyAggs.GetOrAdd(dayKey, _ => new DailyAgg());
            lock (dailyAgg.Sync)
            {
                dailyAgg.Calls += 1;
                dailyAgg.InputTokens += input;
                dailyAgg.OutputTokens += output;
                dailyAgg.TotalTokens += total;
            }
        }

        /// <summary>
        /// Returns current aggregate usage by model and day for one tenant.
        /// </summary>
        /// <param name="dailyDays">The number of recent daily buckets to include.</param>
        /// <param name="tenantId">The tenant bucket to read.</param>
        /// <returns>A live usage snapshot.</returns>
        public AITokenMonitorStats GetLiveStats(int dailyDays = 7, int tenantId = 0)
        {
            _tenantAggs.TryGetValue(tenantId, out var tenant);
            var totalCalls = tenant == null ? 0 : Interlocked.Read(ref tenant.TotalCalls);
            var stats = new AITokenMonitorStats
            {
                TotalCalls = totalCalls,
                SuccessCount = tenant == null ? 0 : Interlocked.Read(ref tenant.SuccessCount),
                ErrorCount = tenant == null ? 0 : Interlocked.Read(ref tenant.ErrorCount),
                TotalInputTokens = tenant == null ? 0 : Interlocked.Read(ref tenant.TotalInputTokens),
                TotalOutputTokens = tenant == null ? 0 : Interlocked.Read(ref tenant.TotalOutputTokens),
                TotalTokens = tenant == null ? 0 : Interlocked.Read(ref tenant.TotalTokens),
                AverageDurationMs = totalCalls > 0
                    ? (double)Interlocked.Read(ref tenant.TotalDurationMs) / totalCalls
                    : 0
            };

            if (tenant != null)
            {
                foreach (var pair in tenant.ModelAggs)
                {
                    var agg = pair.Value;
                    long calls, input, output, total;
                    lock (agg.Sync)
                    {
                        calls = agg.Calls;
                        input = agg.InputTokens;
                        output = agg.OutputTokens;
                        total = agg.TotalTokens;
                    }
                    stats.ByModel.Add(new AITokenModelStat
                    {
                        ModelAlias = pair.Key,
                        Calls = calls,
                        InputTokens = input,
                        OutputTokens = output,
                        TotalTokens = total
                    });
                }
            }
            stats.ByModel.Sort((a, b) => b.TotalTokens.CompareTo(a.TotalTokens));

            for (int i = Math.Max(0, dailyDays - 1); i >= 0; i--)
            {
                var day = DateTime.Now.Date.AddDays(-i);
                var key = day.ToString("yyyy-MM-dd");
                long dcalls = 0, dinput = 0, dout = 0, dtotal = 0;
                if (tenant != null && tenant.DailyAggs.TryGetValue(key, out var agg))
                {
                    lock (agg.Sync)
                    {
                        dcalls = agg.Calls;
                        dinput = agg.InputTokens;
                        dout = agg.OutputTokens;
                        dtotal = agg.TotalTokens;
                    }
                }
                stats.Daily.Add(new AITokenDailyStat
                {
                    Date = day,
                    DateText = key,
                    Calls = dcalls,
                    InputTokens = dinput,
                    OutputTokens = dout,
                    TotalTokens = dtotal
                });
            }

            return stats;
        }

        #region Legacy section

        /// <summary>
        /// Publishes a progress event into the run buffer and active subscribers.
        /// </summary>
        /// <param name="progress">The progress event to publish.</param>
        /// <param name="tenantId">The tenant owning the run.</param>
        /// <exception cref="InvalidOperationException">Thrown when the run belongs to another tenant.</exception>
        public void PublishProgress(AITokenProgressEvent progress, int tenantId = 0)
        {
            if (progress == null)
            {
                return;
            }
            progress.Timestamp = DateTime.Now;

            var run = _runs.GetOrAdd(progress.RunId, _ => new RunState { TenantId = tenantId });
            if (run.TenantId != tenantId)
            {
                throw new InvalidOperationException("Token monitor run belongs to another tenant.");
            }
            List<AITokenProgressEvent> buffered;
            lock (run.Sync)
            {
                run.Buffer.Add(progress);
                if (progress.IsComplete)
                {
                    run.IsComplete = true;
                }
                buffered = new List<AITokenProgressEvent>(run.Buffer);
            }

            foreach (var pair in run.Subscribers)
            {
                pair.Value.Writer.TryWrite(progress);
            }

            if (progress.IsComplete)
            {
                foreach (var pair in run.Subscribers)
                {
                    pair.Value.Writer.TryComplete();
                }
                TrimCompletedRuns();
            }
        }

        /// <summary>
        /// Removes completed progress runs when the in-memory registry exceeds its retention limit.
        /// </summary>
        private void TrimCompletedRuns()
        {
            if (_runs.Count <= MaxCompletedRuns)
            {
                return;
            }
            foreach (var pair in _runs)
            {
                if (pair.Value.IsComplete)
                {
                    _runs.TryRemove(pair.Key, out _);
                }
            }
        }

        /// <summary>
        /// Returns the latest buffered progress event for a tenant-owned run.
        /// </summary>
        /// <param name="runId">The monitored run identifier.</param>
        /// <param name="tenantId">The tenant owning the run.</param>
        /// <returns>The latest event, or <see langword="null"/> when unavailable.</returns>
        public AITokenProgressEvent GetLatestProgress(Guid runId, int tenantId = 0)
        {
            if (_runs.TryGetValue(runId, out var run) && run.TenantId == tenantId)
            {
                lock (run.Sync)
                {
                    if (run.Buffer.Count > 0)
                    {
                        return run.Buffer[run.Buffer.Count - 1];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the buffered progress events for a tenant-owned run in publication order.
        /// </summary>
        /// <param name="runId">The monitored run identifier.</param>
        /// <param name="tenantId">The tenant owning the run.</param>
        /// <returns>A snapshot of buffered events, or an empty list when unavailable.</returns>
        public IReadOnlyList<AITokenProgressEvent> GetBufferedProgress(Guid runId, int tenantId = 0)
        {
            if (_runs.TryGetValue(runId, out var run) && run.TenantId == tenantId)
            {
                lock (run.Sync)
                {
                    return new List<AITokenProgressEvent>(run.Buffer);
                }
            }
            return Array.Empty<AITokenProgressEvent>();
        }

        /// <summary>
        /// Streams buffered and newly published progress events for a tenant-owned run.
        /// </summary>
        /// <param name="runId">The monitored run identifier.</param>
        /// <param name="replayBuffered">Whether existing buffered events should be replayed first.</param>
        /// <param name="cancellationToken">Token used to stop the subscription.</param>
        /// <param name="tenantId">The tenant owning the run.</param>
        public async IAsyncEnumerable<AITokenProgressEvent> SubscribeAsync(
            Guid runId,
            bool replayBuffered = true,
            [EnumeratorCancellation] CancellationToken cancellationToken = default,
            int tenantId = 0)
        {
            if (runId == Guid.Empty)
            {
                yield break;
            }

            var run = _runs.GetOrAdd(runId, _ => new RunState { TenantId = tenantId });
            if (run.TenantId != tenantId)
            {
                throw new InvalidOperationException("Token monitor run belongs to another tenant.");
            }
            var subscriptionId = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<AITokenProgressEvent>();
            run.Subscribers[subscriptionId] = channel;

            try
            {
                if (replayBuffered)
                {
                    List<AITokenProgressEvent> buffered;
                    lock (run.Sync)
                    {
                        buffered = new List<AITokenProgressEvent>(run.Buffer);
                    }
                    foreach (var item in buffered)
                    {
                        yield return item;
                        if (item.IsComplete)
                        {
                            yield break;
                        }
                    }
                }

                if (run.IsComplete && !replayBuffered)
                {
                    yield break;
                }

                var reader = channel.Reader;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        yield return item;
                        if (item.IsComplete)
                        {
                            yield break;
                        }
                    }
                }
            }
            finally
            {
                run.Subscribers.TryRemove(subscriptionId, out _);
                try
                {
                    channel.Writer.TryComplete();
                }
                catch
                {
                    // ignore
                }
            }
        }

        #endregion
    }
}
