using Senparc.Ncf.Repository;
using Senparc.Xncf.AIKernel.Models;
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

            existing.Update(dataset.WorkerAlias, dataset.ExternalId, dataset.Name, dataset.Rows, dataset.Sha256, dataset.CreatedUtc);
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
                artifact.FileId, artifact.StorageKey, artifact.ContentSha256);
            await _artifactRepository.SaveChangesAsync();
            return existing;
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
