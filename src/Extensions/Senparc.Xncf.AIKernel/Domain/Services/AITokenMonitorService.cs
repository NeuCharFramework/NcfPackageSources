/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenMonitorService.cs
    文件功能描述：Token 监控服务（实时聚合 + 异步进度发布/订阅）
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

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
    /// Token 监控服务（进程内单例）。
    /// <para>
    /// 1、实时聚合：以线程安全方式累计 Token 使用量（总体 / 按模型 / 按天）。
    /// 2、异步进度：维护"运行中"的模型调用，通过 <see cref="System.Threading.Channels.Channel{T}"/>
    /// 向订阅者发布 <see cref="AITokenProgressEvent"/>，支持以 <see cref="IAsyncEnumerable{T}"/>
    /// 形式消费（带缓冲回放），实现非阻塞的异步进度推送。
    /// </para>
    /// </summary>
    public sealed class AITokenMonitorService
    {
        #region 实时聚合

        private long _totalCalls;
        private long _successCount;
        private long _errorCount;
        private long _totalInputTokens;
        private long _totalOutputTokens;
        private long _totalTokens;
        private long _totalDurationMs;

        private readonly ConcurrentDictionary<string, ModelAgg> _modelAggs = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DailyAgg> _dailyAggs = new(StringComparer.Ordinal);

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

        #region 异步进度（运行中会话）

        private sealed class RunState
        {
            public readonly object Sync = new();
            public readonly List<AITokenProgressEvent> Buffer = new();
            public readonly ConcurrentDictionary<Guid, Channel<AITokenProgressEvent>> Subscribers = new();
            public bool IsComplete;
        }

        private readonly ConcurrentDictionary<Guid, RunState> _runs = new();

        /// <summary>
        /// 缓冲保留的已结束运行数量上限（防止无限增长）
        /// </summary>
        private const int MaxCompletedRuns = 256;

        #endregion

        /// <summary>
        /// 记录一次完成的 Token 使用（更新实时聚合）。
        /// </summary>
        /// <param name="modelAlias">模型代号</param>
        /// <param name="snapshot">Token 使用快照</param>
        /// <param name="durationMs">调用耗时（毫秒）</param>
        /// <param name="success">是否成功</param>
        public void Record(string modelAlias, AITokenUsageSnapshot snapshot, int durationMs, bool success)
        {
            var input = Math.Max(0, snapshot?.InputTokens ?? 0);
            var output = Math.Max(0, snapshot?.OutputTokens ?? 0);
            var total = Math.Max(0, snapshot?.TotalTokens ?? 0);
            if (total <= 0)
            {
                total = input + output;
            }
            var ms = Math.Max(0, durationMs);

            Interlocked.Increment(ref _totalCalls);
            Interlocked.Add(ref _totalInputTokens, input);
            Interlocked.Add(ref _totalOutputTokens, output);
            Interlocked.Add(ref _totalTokens, total);
            Interlocked.Add(ref _totalDurationMs, ms);
            if (success)
            {
                Interlocked.Increment(ref _successCount);
            }
            else
            {
                Interlocked.Increment(ref _errorCount);
            }

            var key = string.IsNullOrWhiteSpace(modelAlias) ? "(unknown)" : modelAlias;
            var modelAgg = _modelAggs.GetOrAdd(key, _ => new ModelAgg());
            lock (modelAgg.Sync)
            {
                modelAgg.Calls += 1;
                modelAgg.InputTokens += input;
                modelAgg.OutputTokens += output;
                modelAgg.TotalTokens += total;
            }

            var dayKey = DateTime.Now.ToString("yyyy-MM-dd");
            var dailyAgg = _dailyAggs.GetOrAdd(dayKey, _ => new DailyAgg());
            lock (dailyAgg.Sync)
            {
                dailyAgg.Calls += 1;
                dailyAgg.InputTokens += input;
                dailyAgg.OutputTokens += output;
                dailyAgg.TotalTokens += total;
            }
        }

        /// <summary>
        /// 获取当前实时聚合统计。
        /// </summary>
        /// <param name="dailyDays">按天聚合保留的天数（默认 7 天）</param>
        public AITokenMonitorStats GetLiveStats(int dailyDays = 7)
        {
            var stats = new AITokenMonitorStats
            {
                TotalCalls = Interlocked.Read(ref _totalCalls),
                SuccessCount = Interlocked.Read(ref _successCount),
                ErrorCount = Interlocked.Read(ref _errorCount),
                TotalInputTokens = Interlocked.Read(ref _totalInputTokens),
                TotalOutputTokens = Interlocked.Read(ref _totalOutputTokens),
                TotalTokens = Interlocked.Read(ref _totalTokens),
                AverageDurationMs = Interlocked.Read(ref _totalCalls) > 0
                    ? (double)Interlocked.Read(ref _totalDurationMs) / Interlocked.Read(ref _totalCalls)
                    : 0
            };

            foreach (var pair in _modelAggs)
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
            stats.ByModel.Sort((a, b) => b.TotalTokens.CompareTo(a.TotalTokens));

            for (int i = Math.Max(0, dailyDays - 1); i >= 0; i--)
            {
                var day = DateTime.Now.Date.AddDays(-i);
                var key = day.ToString("yyyy-MM-dd");
                long dcalls = 0, dinput = 0, dout = 0, dtotal = 0;
                if (_dailyAggs.TryGetValue(key, out var agg))
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

        #region 异步进度发布 / 订阅

        /// <summary>
        /// 发布一次进度事件（写入缓冲并广播给所有订阅者）。
        /// </summary>
        public void PublishProgress(AITokenProgressEvent progress)
        {
            if (progress == null)
            {
                return;
            }
            progress.Timestamp = DateTime.Now;

            var run = _runs.GetOrAdd(progress.RunId, _ => new RunState());
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
        /// 获取某次运行的最新进度（用于轮询式异步进度）。
        /// </summary>
        public AITokenProgressEvent GetLatestProgress(Guid runId)
        {
            if (_runs.TryGetValue(runId, out var run))
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
        /// 获取某次运行的全部缓冲进度（用于重放）。
        /// </summary>
        public IReadOnlyList<AITokenProgressEvent> GetBufferedProgress(Guid runId)
        {
            if (_runs.TryGetValue(runId, out var run))
            {
                lock (run.Sync)
                {
                    return new List<AITokenProgressEvent>(run.Buffer);
                }
            }
            return Array.Empty<AITokenProgressEvent>();
        }

        /// <summary>
        /// 订阅某次运行的异步进度（IAsyncEnumerable，支持回放缓冲 + 实时增量）。
        /// </summary>
        /// <param name="runId">运行唯一标识</param>
        /// <param name="replayBuffered">是否先回放已产生的缓冲事件</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async IAsyncEnumerable<AITokenProgressEvent> SubscribeAsync(
            Guid runId,
            bool replayBuffered = true,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (runId == Guid.Empty)
            {
                yield break;
            }

            var run = _runs.GetOrAdd(runId, _ => new RunState());
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
