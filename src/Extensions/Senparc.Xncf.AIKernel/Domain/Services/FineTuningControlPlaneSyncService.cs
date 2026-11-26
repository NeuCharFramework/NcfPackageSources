/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningControlPlaneSyncService.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261028
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance event synchronization with concurrent cursor management

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

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

    /// <summary>
    /// Initializes periodic cross-tenant Worker-to-control-plane synchronization.
    /// </summary>
    /// <param name="tenantScopeFactory">The enabled-tenant scope factory.</param>
    /// <param name="logger">The synchronization logger.</param>
    /// <param name="options">Fine-tuning worker options.</param>
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
        try
        {
            await SynchronizeAsync(stoppingToken);
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SynchronizeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Fine-tuning control-plane synchronization stopped.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Fine-tuning control-plane synchronization loop stopped unexpectedly.");
        }
    }

    /// <summary>
    /// Synchronizes all enabled Workers across enabled tenants.
    /// </summary>
    /// <param name="stoppingToken">Token used to stop synchronization.</param>
    /// <returns>A task that completes after one synchronization pass.</returns>
    private async Task SynchronizeAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        try
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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

    /// <summary>
    /// Synchronizes jobs, artifacts, and incremental events for one Worker.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="worker">The Worker API client.</param>
    /// <param name="controlPlane">The local control plane.</param>
    /// <param name="eventCursors">The shared per-job event cursors.</param>
    /// <param name="cancellationToken">Token used to cancel synchronization.</param>
    /// <returns>A task that completes after the Worker pass.</returns>
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
