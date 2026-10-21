/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningMigrationService.cs
    文件功能描述：执行微调产物迁移
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 执行微调产物迁移

----------------------------------------------------------------*/

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningMigrationService
{
    public const string MigrationKey = "legacy-worker-metadata-v1";
    private const string TargetVersion = "aikernel-ncf";
    private const int PageSize = 100;
    private const int EventPageSize = 200;

    private readonly FineTuningWorkerClient _worker;
    private readonly IFineTuningControlPlaneService _controlPlane;
    private readonly IFineTuningFileStore _fileStore;

    public FineTuningMigrationService(FineTuningWorkerClient worker,
        IFineTuningControlPlaneService controlPlane, IFineTuningFileStore fileStore)
    {
        _worker = worker;
        _controlPlane = controlPlane;
        _fileStore = fileStore;
    }

    public async Task<FineTuningMigrationResult> MigrateAsync(string workerAlias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerAlias))
            throw new ArgumentException("Worker alias is required.", nameof(workerAlias));

        var startedUtc = DateTime.UtcNow;
        var health = await _worker.HealthAsync(workerAlias, cancellationToken);
        var storeId = string.IsNullOrWhiteSpace(health.StoreId) ? workerAlias : health.StoreId;
        var migration = new AIFineTuningMigration(workerAlias, storeId, MigrationKey, "Running",
            health.Version, TargetVersion, startedUtc, null, string.Empty);
        await _controlPlane.UpsertMigrationAsync(migration, cancellationToken);

        var result = new FineTuningMigrationResult
        {
            WorkerAlias = workerAlias,
            StoreId = storeId,
            Status = "Running"
        };

        try
        {
            var datasetOffset = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = await _worker.GetDatasetsPageAsync(workerAlias, datasetOffset, PageSize, cancellationToken);
                foreach (var dataset in page.Items)
                {
                    await _controlPlane.UpsertDatasetAsync(new AIFineTuningDataset(workerAlias, dataset.Id,
                        dataset.Name, dataset.Rows, dataset.Sha256, dataset.CreatedUtc.UtcDateTime), cancellationToken);
                    result.Datasets++;
                }

                datasetOffset += page.Items.Count;
                if (page.Items.Count == 0 || datasetOffset >= page.Total)
                    break;
            }

            var jobOffset = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = await _worker.GetJobsPageAsync(workerAlias, jobOffset, PageSize, cancellationToken);
                foreach (var job in page.Items)
                {
                    await MirrorJobAsync(workerAlias, job, result, cancellationToken);
                }

                jobOffset += page.Items.Count;
                if (page.Items.Count == 0 || jobOffset >= page.Total)
                    break;
            }

            result.Status = "Completed";
            result.CompletedUtc = DateTime.UtcNow;
            await _controlPlane.UpsertMigrationAsync(new AIFineTuningMigration(workerAlias, storeId, MigrationKey,
                result.Status, health.Version, TargetVersion, startedUtc, result.CompletedUtc, string.Empty),
                cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            await _controlPlane.UpsertMigrationAsync(new AIFineTuningMigration(workerAlias, storeId, MigrationKey,
                "Cancelled", health.Version, TargetVersion, startedUtc, DateTime.UtcNow, "Migration was cancelled."),
                CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            result.Status = "Failed";
            result.Error = exception.Message;
            await _controlPlane.UpsertMigrationAsync(new AIFineTuningMigration(workerAlias, storeId, MigrationKey,
                result.Status, health.Version, TargetVersion, startedUtc, DateTime.UtcNow, exception.ToString()),
                CancellationToken.None);
            throw;
        }
    }

    private async Task MirrorJobAsync(string workerAlias, FineTuningJob job,
        FineTuningMigrationResult result, CancellationToken cancellationToken)
    {
        await _controlPlane.UpsertJobAsync(new AIFineTuningJob(workerAlias, job.Id, job.Name, job.State,
            job.CreatedUtc.UtcDateTime, job.StartedUtc?.UtcDateTime, job.FinishedUtc?.UtcDateTime,
            JsonSerializer.Serialize(job.Request), job.Error ?? string.Empty,
            JsonSerializer.Serialize(job.LatestMetrics), job.LastEventSequence), cancellationToken);
        result.Jobs++;

        foreach (var artifact in job.Artifacts)
        {
            var existing = await _controlPlane.FindArtifactAsync(workerAlias, job.Id, artifact.Id, cancellationToken);
            if (existing?.FileId is null)
            {
                if (!await _fileStore.IsAvailableAsync(cancellationToken))
                {
                    throw new InvalidOperationException("FileManager is required to migrate training artifacts.");
                }

                using var response = await _worker.DownloadArtifactAsync(workerAlias, job.Id, artifact.Id, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var imported = await _fileStore.ImportAsync(stream, artifact.Name,
                    response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                    response.Content.Headers.ContentLength ?? artifact.Bytes,
                    $"AIKernel fine-tuning artifact {workerAlias}/{job.Id}/{artifact.Id}",
                    cancellationToken);
                await _controlPlane.UpsertArtifactAsync(new AIFineTuningArtifact(workerAlias, job.Id, artifact.Id,
                    artifact.Name, artifact.Bytes, imported.FileId, null, null), cancellationToken);
            }
            else
            {
                await _controlPlane.UpsertArtifactAsync(new AIFineTuningArtifact(workerAlias, job.Id, artifact.Id,
                    artifact.Name, artifact.Bytes, existing.FileId, existing.StorageKey, existing.ContentSha256),
                    cancellationToken);
            }
            result.Artifacts++;
        }

        var after = 0L;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = await _worker.GetEventsAsync(workerAlias, job.Id, after, EventPageSize, cancellationToken);
            foreach (var item in page.Events)
            {
                await _controlPlane.UpsertEventAsync(new AIFineTuningEvent(workerAlias, job.Id, item.Sequence,
                    item.TimestampUtc.UtcDateTime, item.Kind, item.Message ?? string.Empty,
                    JsonSerializer.Serialize(item.Metrics)), cancellationToken);
                result.Events++;
            }

            if (!page.HasMore || page.Events.Count == 0 || page.NextCursor <= after)
                break;
            after = page.NextCursor;
        }
    }
}
