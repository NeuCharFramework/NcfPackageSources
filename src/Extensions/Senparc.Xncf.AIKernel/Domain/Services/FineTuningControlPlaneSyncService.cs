/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningControlPlaneSyncService.cs
    文件功能描述：同步微调控制面与外部训练状态
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 同步微调控制面与外部训练状态

    修改标识：Senparc - 20261028
    修改描述：多租户模式下按租户迭代同步微调控制面，避免根作用域缺失租户上下文

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance event synchronization with concurrent cursor management

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningControlPlaneSyncService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IBackgroundTenantScopeFactory _tenantScopeFactory;
    private readonly ILogger<FineTuningControlPlaneSyncService> _logger;
    private readonly IOptions<FineTuningWorkerOptions> _options;
    private readonly ConcurrentDictionary<string, long> _eventCursors = new();

    public FineTuningControlPlaneSyncService(IBackgroundTenantScopeFactory tenantScopeFactory,
        ILogger<FineTuningControlPlaneSyncService> logger,
        IOptions<FineTuningWorkerOptions> options)
    {
        _tenantScopeFactory = tenantScopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SynchronizeAsync(stoppingToken);
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SynchronizeAsync(stoppingToken);
        }
    }

    private async Task SynchronizeAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        try
        {
            // 微调工作器与任务数据均挂在租户下，后台同步必须按租户迭代，
            // 否则根作用域缺少租户上下文（多租户模式下 fail-closed 或写入错误租户）。
            await _tenantScopeFactory.ForEachEnabledTenantAsync(
                async (scopeServiceProvider, ct) =>
                {
                    var configuration = scopeServiceProvider.GetRequiredService<IFineTuningWorkerConfigurationProvider>();
                    var worker = scopeServiceProvider.GetRequiredService<FineTuningWorkerClient>();
                    var controlPlane = scopeServiceProvider.GetRequiredService<IFineTuningControlPlaneService>();
                    var workers = await configuration.GetWorkersAsync(ct).ConfigureAwait(false);

                    foreach (var profile in workers)
                    {
                        if (!profile.Enabled || !profile.HasConfiguredSecret)
                        {
                            continue;
                        }

                        await SynchronizeWorkerAsync(profile.Alias, worker, controlPlane, _eventCursors, ct).ConfigureAwait(false);
                    }
                },
                stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Fine-tuning control-plane synchronization failed.");
        }
    }

    private static async Task SynchronizeWorkerAsync(string workerAlias, FineTuningWorkerClient worker,
        IFineTuningControlPlaneService controlPlane, ConcurrentDictionary<string, long> eventCursors,
        CancellationToken cancellationToken)
    {
        var jobs = await worker.GetJobsPageAsync(workerAlias, 0, 50, cancellationToken);
        foreach (var job in jobs.Items)
        {
            await controlPlane.UpsertJobAsync(new AIFineTuningJob(workerAlias, job.Id, job.Name, job.State,
                job.CreatedUtc.UtcDateTime, job.StartedUtc?.UtcDateTime, job.FinishedUtc?.UtcDateTime,
                System.Text.Json.JsonSerializer.Serialize(job.Request), job.Error ?? string.Empty,
                System.Text.Json.JsonSerializer.Serialize(job.LatestMetrics), job.LastEventSequence),
                cancellationToken);

            foreach (var artifact in job.Artifacts)
            {
                await controlPlane.UpsertArtifactAsync(new AIFineTuningArtifact(workerAlias, job.Id, artifact.Id,
                    artifact.Name, artifact.Bytes), cancellationToken);
            }

            var cursorKey = $"{workerAlias}/{job.Id}";
            eventCursors.TryGetValue(cursorKey, out var after);
            var active = job.State is "Queued" or "Running" or "Pausing" or "Paused" or "Resuming" or "Cancelling";
            if (!active && job.LastEventSequence <= after)
            {
                continue;
            }

            var hasMore = true;
            while (hasMore && after < job.LastEventSequence)
            {
                var events = await worker.GetEventsAsync(workerAlias, job.Id, after, 200, cancellationToken);
                foreach (var item in events.Events)
                {
                    await controlPlane.UpsertEventAsync(new AIFineTuningEvent(workerAlias, job.Id, item.Sequence,
                        item.TimestampUtc.UtcDateTime, item.Kind, item.Message ?? string.Empty,
                        System.Text.Json.JsonSerializer.Serialize(item.Metrics)), cancellationToken);
                }

                if (events.NextCursor <= after)
                {
                    break;
                }
                after = events.NextCursor;
                hasMore = events.HasMore;
            }
            eventCursors[cursorKey] = after;
        }
    }
}
