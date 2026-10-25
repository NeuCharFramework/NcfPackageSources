/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningFileReferenceProvider.cs
    文件功能描述：向 FileManager 提供微调文件引用查询

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

    public FineTuningFileReferenceProvider(
        IRepositoryBase<AIFineTuningDataset> datasetRepository,
        IRepositoryBase<AIFineTuningArtifact> artifactRepository)
    {
        _datasetRepository = datasetRepository;
        _artifactRepository = artifactRepository;
    }

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
