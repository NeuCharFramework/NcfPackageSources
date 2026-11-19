/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IFineTuningControlPlaneService.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance job management and telemetry features

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Models;
using System;
using System.Collections.Generic;
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
        Task<List<int>> DeleteJobAsync(string workerAlias, string jobExternalId, CancellationToken cancellationToken = default);
        Task<AIFineTuningMigration> UpsertMigrationAsync(AIFineTuningMigration migration, CancellationToken cancellationToken = default);
    }
}
