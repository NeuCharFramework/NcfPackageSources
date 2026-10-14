using Senparc.Xncf.AIKernel.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public interface IFineTuningControlPlaneService
    {
        Task<AIFineTuningDataset> UpsertDatasetAsync(AIFineTuningDataset dataset, CancellationToken cancellationToken = default);
        Task<AIFineTuningJob> UpsertJobAsync(AIFineTuningJob job, CancellationToken cancellationToken = default);
        Task<AIFineTuningEvent> UpsertEventAsync(AIFineTuningEvent fineTuningEvent, CancellationToken cancellationToken = default);
        Task<AIFineTuningArtifact> UpsertArtifactAsync(AIFineTuningArtifact artifact, CancellationToken cancellationToken = default);
        Task<AIFineTuningArtifact> FindArtifactAsync(string workerAlias, string jobExternalId, string externalId,
            CancellationToken cancellationToken = default);
        Task<AIFineTuningMigration> UpsertMigrationAsync(AIFineTuningMigration migration, CancellationToken cancellationToken = default);
    }
}
