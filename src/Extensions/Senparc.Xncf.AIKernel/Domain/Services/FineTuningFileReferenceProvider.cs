/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningFileReferenceProvider.cs
    File description: Related implementation.

    Created by: Senparc - 20261026

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.FileManager.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningFileReferenceProvider : IFileManagerReferenceProvider
{
    private readonly IRepositoryBase<AIFineTuningDataset> _datasetRepository;
    private readonly IRepositoryBase<AIFineTuningArtifact> _artifactRepository;

    /// <summary>Initializes the FileManager reference provider over dataset/artifact repositories.</summary>
    /// <param name="datasetRepository">The dataset repository.</param>
    /// <param name="artifactRepository">The artifact repository.</param>
    public FineTuningFileReferenceProvider(
        IRepositoryBase<AIFineTuningDataset> datasetRepository,
        IRepositoryBase<AIFineTuningArtifact> artifactRepository)
    {
        _datasetRepository = datasetRepository;
        _artifactRepository = artifactRepository;
    }

    /// <summary>Returns dataset and artifact references for a FileManager file identifier.</summary>
    /// <param name="fileId">The FileManager file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <returns>The business references preventing unsafe file deletion.</returns>
    public async Task<IReadOnlyList<FileManagerReference>> GetReferencesAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var references = new List<FileManagerReference>();

        var datasets = await _datasetRepository.GetObjectListAsync(
            item => item.FileId == fileId, item => item.Id, OrderingType.Ascending, 0, 0);
        references.AddRange(datasets.Select(dataset => new FileManagerReference(
            fileId,
            "FineTuningDataset",
            $"{dataset.WorkerAlias}/{dataset.ExternalId}",
            $"{dataset.Name} ({dataset.Rows} rows)")));

        var artifacts = await _artifactRepository.GetObjectListAsync(
            item => item.FileId == fileId, item => item.Id, OrderingType.Ascending, 0, 0);
        references.AddRange(artifacts.Select(artifact => new FileManagerReference(
            fileId,
            "FineTuningArtifact",
            $"{artifact.WorkerAlias}/{artifact.JobExternalId}/{artifact.ExternalId}",
            artifact.Name)));

        cancellationToken.ThrowIfCancellationRequested();
        return references;
    }
}
