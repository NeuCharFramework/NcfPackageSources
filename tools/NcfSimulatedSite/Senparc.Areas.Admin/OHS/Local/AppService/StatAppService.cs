/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: StatAppService.cs
    File description: StatAppService.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260809
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260926
    Change description: v0.12.0 Updated module behavior and related support.

    Modified by: Senparc - 20261102
    Change description: v0.12.3 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add streamed large-file Workspace uploads with tenant-aware ownership checks

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.OHS.Local.PL;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.SenparcTraceManager;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class StatAppService : LocalAppServiceBase
    {
        private readonly HostMetricsCollector _hostMetricsCollector;

        public StatAppService(
            IServiceProvider serviceProvider,
            HostMetricsCollector hostMetricsCollector) : base(serviceProvider)
        {
            _hostMetricsCollector = hostMetricsCollector;
        }

        [ApiBind]
        public async Task<AppResponseBase<Stat_GetLogsResponse>> GetLogs()
        {
            return await this.GetResponseAsync<Stat_GetLogsResponse>(async (response, logger) =>
            {
                var result = new Stat_GetLogsResponse();
                var dates = SenparcTraceHelper.GetLogDate().Take(14).OrderBy(z => z).ToList();
                foreach (var date in dates)
                {
                    //Legacy comment translated to English.
                    var scan = await SenparcTraceHelper.GetLogScanResultAsync(base.ServiceProvider, date);
                    result.Logs.Add(new Stat_GetLogsResponse_Item()
                    {
                        Date = date,
                        ExceptionLogCount = scan.ExceptionLogCount,
                        NormalLogCount = scan.NormalLogCount,
                        TotalLogCount = scan.TotalLogCount
                    });
                    logger.Append($"GetLogs date={date}, total={scan.TotalLogCount}, exception={scan.ExceptionLogCount}");
                }
                return result;
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<Stat_GetTodayLogResponse>> GetTodayLog()
        {
            return await this.GetResponseAsync<Stat_GetTodayLogResponse>(async (response, logger) =>
            {
                var result = new Stat_GetTodayLogResponse()
                {
                    Date = SystemTime.Now.ToString("yyyyMMdd")
                };

                var todayLogPath = Path.Combine(SenparcTraceHelper.DefaultLogPath, $"SenparcTrace-{result.Date}.log");
                if (!File.Exists(todayLogPath))
                {
                    logger.Append($"GetTodayLog date={result.Date}, file missing, return empty");
                    return result;
                }

                var scan = await SenparcTraceHelper.GetLogScanResultAsync(base.ServiceProvider, result.Date);
                foreach (var item in scan.TypeCounts.OrderBy(z => z.Key.ToString()))
                {
                    result.Items.Add(new Stat_GetTodayLogResponse_Item()
                    {
                        SenparcTraceType = item.Key.ToString(),
                        Count = item.Value
                    });
                }

                logger.Append($"GetTodayLog date={result.Date}, types={result.Items.Count}, total={scan.TotalLogCount}");
                return result;
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<ResourceMonitorData>> GetResourceMonitor()
        {
            return await this.GetResponseAsync<ResourceMonitorData>(async (response, logger) =>
            {
                var cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
                var windows = await Task.WhenAll(
                    ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyFine, "fine", 10),
                    ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyMid, "mid", 60),
                    ReadResourceMonitorWindowAsync(cache, ResourceMonitorRecorder.CacheKeyCoarse, "coarse", 300))
                    .ConfigureAwait(false);
                var data = new ResourceMonitorData
                {
                    Now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    SampleIntervalSeconds = ResourceMonitorRecorder.SampleIntervalSeconds,
                    Windows = windows.ToList()
                };
                logger.Append($"GetResourceMonitor fine={data.Windows[0].Points.Count}, mid={data.Windows[1].Points.Count}, coarse={data.Windows[2].Points.Count}");
                return data;
            });
        }

        private static async Task<ResourceMonitorWindow> ReadResourceMonitorWindowAsync(
            IBaseObjectCacheStrategy cache,
            string key,
            string windowKey,
            int resolutionSeconds)
        {
            var window = await cache.GetAsync<ResourceMonitorWindow>(key).ConfigureAwait(false);
            if (window == null)
            {
                window = new ResourceMonitorWindow
                {
                    Key = windowKey,
                    ResolutionSeconds = resolutionSeconds
                };
            }
            if (window.Points == null)
            {
                window.Points = new List<ResourceMonitorPoint>();
            }
            return window;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<HostMetricsSnapshot>> GetHostMetrics()
        {
            return await this.GetResponseAsync<HostMetricsSnapshot>((response, logger) =>
            {
                return Task.FromResult(_hostMetricsCollector.Collect());
            });
        }
    }
}
