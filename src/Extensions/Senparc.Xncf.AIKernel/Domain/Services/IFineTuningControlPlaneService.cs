/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IFineTuningControlPlaneService.cs
    文件功能描述：微调控制面服务契约
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 定义微调控制面服务契约

----------------------------------------------------------------*/

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
