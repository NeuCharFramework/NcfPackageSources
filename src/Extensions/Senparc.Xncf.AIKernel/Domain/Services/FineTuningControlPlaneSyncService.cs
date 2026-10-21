/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningControlPlaneSyncService.cs
    文件功能描述：同步微调控制面与外部训练状态
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 同步微调控制面与外部训练状态

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningControlPlaneSyncService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FineTuningControlPlaneSyncService> _logger;
    private readonly IOptions<FineTuningWorkerOptions> _options;

    public FineTuningControlPlaneSyncService(IServiceScopeFactory scopeFactory,
        ILogger<FineTuningControlPlaneSyncService> logger,
        IOptions<FineTuningWorkerOptions> options)
    {
        _scopeFactory = scopeFactory;
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
            using var scope = _scopeFactory.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IFineTuningWorkerConfigurationProvider>();
            var worker = scope.ServiceProvider.GetRequiredService<FineTuningWorkerClient>();
            var controlPlane = scope.ServiceProvider.GetRequiredService<IFineTuningControlPlaneService>();
            var workers = await configuration.GetWorkersAsync(stoppingToken);

            foreach (var profile in workers)
            {
                if (!profile.Enabled || !profile.HasConfiguredSecret)
                {
                    continue;
                }

                await SynchronizeWorkerAsync(profile.Alias, worker, controlPlane, stoppingToken);
            }
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
        IFineTuningControlPlaneService controlPlane, CancellationToken cancellationToken)
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

            var events = await worker.GetEventsAsync(workerAlias, job.Id, 0, 200, cancellationToken);
            foreach (var item in events.Events)
            {
                await controlPlane.UpsertEventAsync(new AIFineTuningEvent(workerAlias, job.Id, item.Sequence,
                    item.TimestampUtc.UtcDateTime, item.Kind, item.Message ?? string.Empty,
                    System.Text.Json.JsonSerializer.Serialize(item.Metrics)), cancellationToken);
            }
        }
    }
}
