/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningControlPlaneService.cs
    文件功能描述：微调任务、数据集与产物控制面
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 提供微调任务、数据集与产物控制面

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance job management and telemetry features

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public class FineTuningControlPlaneService : IFineTuningControlPlaneService
    {
        private readonly IRepositoryBase<AIFineTuningDataset> _datasetRepository;
        private readonly IRepositoryBase<AIFineTuningJob> _jobRepository;
        private readonly IRepositoryBase<AIFineTuningEvent> _eventRepository;
        private readonly IRepositoryBase<AIFineTuningArtifact> _artifactRepository;
        private readonly IRepositoryBase<AIFineTuningMigration> _migrationRepository;

        public FineTuningControlPlaneService(
            IRepositoryBase<AIFineTuningDataset> datasetRepository,
            IRepositoryBase<AIFineTuningJob> jobRepository,
            IRepositoryBase<AIFineTuningEvent> eventRepository,
            IRepositoryBase<AIFineTuningArtifact> artifactRepository,
            IRepositoryBase<AIFineTuningMigration> migrationRepository)
        {
            _datasetRepository = datasetRepository;
            _jobRepository = jobRepository;
            _eventRepository = eventRepository;
            _artifactRepository = artifactRepository;
            _migrationRepository = migrationRepository;
        }

        public async Task<AIFineTuningDataset> UpsertDatasetAsync(AIFineTuningDataset dataset, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _datasetRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == dataset.WorkerAlias && item.ExternalId == dataset.ExternalId);
            if (existing == null)
            {
                await _datasetRepository.SaveAsync(dataset);
                return dataset;
            }

            if (dataset.FileId.HasValue)
            {
                existing.Update(dataset.WorkerAlias, dataset.ExternalId, dataset.Name, dataset.Rows, dataset.Sha256,
                    dataset.CreatedUtc, dataset.FileId);
            }
            else
            {
                existing.Update(dataset.WorkerAlias, dataset.ExternalId, dataset.Name, dataset.Rows, dataset.Sha256,
                    dataset.CreatedUtc);
            }
            await _datasetRepository.SaveChangesAsync();
            return existing;
        }

        public async Task<AIFineTuningJob> UpsertJobAsync(AIFineTuningJob job, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _jobRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == job.WorkerAlias && item.ExternalId == job.ExternalId);
            if (existing == null)
            {
                await _jobRepository.SaveAsync(job);
                return job;
            }

            existing.Update(job.WorkerAlias, job.ExternalId, job.Name, job.State, job.CreatedUtc, job.StartedUtc,
                job.FinishedUtc, job.RequestJson, job.Error, job.LatestMetricsJson, job.LastEventSequence);
            await _jobRepository.SaveChangesAsync();
            return existing;
        }

        public async Task<AIFineTuningEvent> UpsertEventAsync(AIFineTuningEvent fineTuningEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _eventRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == fineTuningEvent.WorkerAlias &&
                item.JobExternalId == fineTuningEvent.JobExternalId &&
                item.Sequence == fineTuningEvent.Sequence);
            if (existing == null)
            {
                await _eventRepository.SaveAsync(fineTuningEvent);
                return fineTuningEvent;
            }

            existing.Update(fineTuningEvent.WorkerAlias, fineTuningEvent.JobExternalId, fineTuningEvent.Sequence,
                fineTuningEvent.TimestampUtc, fineTuningEvent.Kind, fineTuningEvent.Message, fineTuningEvent.MetricsJson);
            await _eventRepository.SaveChangesAsync();
            return existing;
        }

        public async Task<AIFineTuningArtifact> UpsertArtifactAsync(AIFineTuningArtifact artifact, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _artifactRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == artifact.WorkerAlias &&
                item.JobExternalId == artifact.JobExternalId &&
                item.ExternalId == artifact.ExternalId);
            if (existing == null)
            {
                await _artifactRepository.SaveAsync(artifact);
                return artifact;
            }

            existing.Update(artifact.WorkerAlias, artifact.JobExternalId, artifact.ExternalId, artifact.Name, artifact.Bytes,
                artifact.FileId ?? existing.FileId, artifact.StorageKey ?? existing.StorageKey,
                artifact.ContentSha256 ?? existing.ContentSha256);
            await _artifactRepository.SaveChangesAsync();
            return existing;
        }

        public async Task<List<int>> DeleteJobAsync(string workerAlias, string jobExternalId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var artifacts = (await _artifactRepository.GetObjectListAsync(
                item => item.WorkerAlias == workerAlias && item.JobExternalId == jobExternalId,
                item => item.Id, OrderingType.Ascending, 0, 0)).ToList();
            var events = (await _eventRepository.GetObjectListAsync(
                item => item.WorkerAlias == workerAlias && item.JobExternalId == jobExternalId,
                item => item.Id, OrderingType.Ascending, 0, 0)).ToList();
            var job = await _jobRepository.GetFirstOrDefaultObjectAsync(
                item => item.WorkerAlias == workerAlias && item.ExternalId == jobExternalId);

            if (artifacts.Count > 0)
            {
                await _artifactRepository.DeleteAllAsync(artifacts);
            }
            if (events.Count > 0)
            {
                await _eventRepository.DeleteAllAsync(events);
            }
            if (job != null)
            {
                await _jobRepository.DeleteAsync(job);
            }

            return artifacts.Where(item => item.FileId.HasValue).Select(item => item.FileId!.Value).Distinct().ToList();
        }

        public Task<AIFineTuningArtifact> FindArtifactAsync(string workerAlias, string jobExternalId,
            string externalId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _artifactRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == workerAlias &&
                item.JobExternalId == jobExternalId &&
                item.ExternalId == externalId);
        }

        public async Task<AIFineTuningMigration> UpsertMigrationAsync(AIFineTuningMigration migration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await _migrationRepository.GetFirstOrDefaultObjectAsync(item =>
                item.WorkerAlias == migration.WorkerAlias &&
                item.StoreId == migration.StoreId &&
                item.MigrationKey == migration.MigrationKey);
            if (existing == null)
            {
                await _migrationRepository.SaveAsync(migration);
                return migration;
            }

            existing.Update(migration.WorkerAlias, migration.StoreId, migration.MigrationKey, migration.Status,
                migration.SourceVersion, migration.TargetVersion, migration.StartedUtc, migration.CompletedUtc, migration.Error);
            await _migrationRepository.SaveChangesAsync();
            return existing;
        }
    }
}
