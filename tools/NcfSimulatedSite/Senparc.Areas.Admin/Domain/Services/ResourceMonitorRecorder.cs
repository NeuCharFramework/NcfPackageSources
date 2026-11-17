/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ResourceMonitorRecorder.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Change description: v0.12.0 Updated module behavior and related support.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261102
    Change description: v0.12.7 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add PromptRange selector service, UI components, and tests

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.CO2NET.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class ResourceMonitorRecorder : BackgroundService
    {
        public const string CacheKeyFine = "NcfResourceMonitor:fine";
        public const string CacheKeyMid = "NcfResourceMonitor:mid";
        public const string CacheKeyCoarse = "NcfResourceMonitor:coarse";

        ///Legacy comment translated to English.
        public const int SampleIntervalSeconds = 10;
        ///Legacy comment translated to English.
        public const int FineMaxPoints = 180;
        ///Legacy comment translated to English.
        public const int MidMaxPoints = 360;
        ///Legacy comment translated to English.
        public const int CoarseMaxPoints = 288;

        private static readonly TimeSpan FineTtl = TimeSpan.FromMinutes(40);
        private static readonly TimeSpan MidTtl = TimeSpan.FromHours(7);
        private static readonly TimeSpan CoarseTtl = TimeSpan.FromHours(25);

        private readonly IBaseObjectCacheStrategy _cache;
        private readonly ILogger<ResourceMonitorRecorder> _logger;

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        private readonly HostMetricsCollector _collector = new();

        //Legacy comment translated to English.
        private readonly List<ResourceMonitorPoint> _pendingMinute = new();
        private readonly List<ResourceMonitorPoint> _recentMinutes = new();
        private long _currentMinuteIndex;
        private long _currentFiveMinuteIndex;

        public ResourceMonitorRecorder(ILogger<ResourceMonitorRecorder> logger)
        {
            _logger = logger;
            _cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Legacy comment translated to English.
            try
            {
                _collector.Collect();
                await Task.Delay(TimeSpan.FromSeconds(SampleIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(SampleIntervalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    try
                    {
                        await RecordTickAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        //Legacy comment translated to English.
                        _logger.LogWarning(ex, "资源监控采样失败，等待下一个周期。");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Legacy comment translated to English.
            }
        }

        private async Task RecordTickAsync()
        {
            var snapshot = _collector.Collect();
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var point = new ResourceMonitorPoint
            {
                T = nowMs,
                Cpu = snapshot.CpuUsagePercent,
                Pcpu = snapshot.ProcessCpuUsagePercent,
                Mem = snapshot.MemoryUsagePercent,
                Pmem = snapshot.MemoryTotalBytes > 0
                    ? Math.Round(snapshot.ProcessWorkingSetBytes * 100d / snapshot.MemoryTotalBytes, 2)
                    : (double?)null,
                Rx = snapshot.NetworkReceiveBytesPerSecond.HasValue
                    ? Math.Round(snapshot.NetworkReceiveBytesPerSecond.Value * 8d / 1000000d, 3)
                    : (double?)null,
                Tx = snapshot.NetworkSendBytesPerSecond.HasValue
                    ? Math.Round(snapshot.NetworkSendBytesPerSecond.Value * 8d / 1000000d, 3)
                    : (double?)null
            };

            var minuteIndex = nowMs / 60000;
            var fiveMinuteIndex = nowMs / 300000;

            if (_currentMinuteIndex != 0 && minuteIndex != _currentMinuteIndex)
            {
                await FinalizeMinuteAsync().ConfigureAwait(false);
            }

            if (_currentFiveMinuteIndex != 0 && fiveMinuteIndex != _currentFiveMinuteIndex)
            {
                await FinalizeFiveMinutesAsync().ConfigureAwait(false);
            }

            _currentMinuteIndex = minuteIndex;
            _currentFiveMinuteIndex = fiveMinuteIndex;

            _pendingMinute.Add(point);
            await AppendWindowAsync(
                CacheKeyFine, "fine", 10, point, FineMaxPoints, FineTtl).ConfigureAwait(false);
        }

        private async Task FinalizeMinuteAsync()
        {
            if (_pendingMinute.Count == 0)
            {
                return;
            }

            var minutePoint = Aggregate(_pendingMinute, _pendingMinute[0].T / 60000 * 60000);
            _pendingMinute.Clear();

            _recentMinutes.Add(minutePoint);
            if (_recentMinutes.Count > 6)
            {
                _recentMinutes.RemoveRange(0, _recentMinutes.Count - 6);
            }

            await AppendWindowAsync(
                CacheKeyMid, "mid", 60, minutePoint, MidMaxPoints, MidTtl).ConfigureAwait(false);
        }

        private async Task FinalizeFiveMinutesAsync()
        {
            if (_recentMinutes.Count == 0)
            {
                return;
            }

            var window = _recentMinutes.TakeLast(5).ToList();
            var fiveMinutePoint = Aggregate(window, window[0].T / 300000 * 300000);
            await AppendWindowAsync(
                CacheKeyCoarse, "coarse", 300, fiveMinutePoint, CoarseMaxPoints, CoarseTtl).ConfigureAwait(false);
        }

        private static ResourceMonitorPoint Aggregate(
            List<ResourceMonitorPoint> source, long timestamp)
        {
            return new ResourceMonitorPoint
            {
                T = timestamp,
                Cpu = Average(source, point => point.Cpu),
                Pcpu = Average(source, point => point.Pcpu),
                Mem = Average(source, point => point.Mem),
                Pmem = Average(source, point => point.Pmem),
                Rx = Average(source, point => point.Rx),
                Tx = Average(source, point => point.Tx)
            };
        }

        private static double? Average(
            List<ResourceMonitorPoint> source,
            Func<ResourceMonitorPoint, double?> selector)
        {
            double sum = 0;
            var count = 0;
            foreach (var point in source)
            {
                var value = selector(point);
                if (value.HasValue)
                {
                    sum += value.Value;
                    count++;
                }
            }
            return count > 0 ? Math.Round(sum / count, 2) : (double?)null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private async Task AppendWindowAsync(
            string key,
            string windowKey,
            int resolutionSeconds,
            ResourceMonitorPoint point,
            int maxPoints,
            TimeSpan ttl)
        {
            var window = await _cache.GetAsync<ResourceMonitorWindow>(key).ConfigureAwait(false);
            if (window == null)
            {
                window = new ResourceMonitorWindow
                {
                    Key = windowKey,
                    ResolutionSeconds = resolutionSeconds
                };
            }
            else if (window.Points == null)
            {
                window.Points = new List<ResourceMonitorPoint>();
            }

            if (window.Points.Count == 0 || point.T > window.Points[window.Points.Count - 1].T)
            {
                window.Points.Add(point);
            }

            if (window.Points.Count > maxPoints)
            {
                window.Points.RemoveRange(0, window.Points.Count - maxPoints);
            }

            await _cache.SetAsync(key, window, ttl).ConfigureAwait(false);
        }
    }
}
