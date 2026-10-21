/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IModelPreparationService.cs
    文件功能描述：模型准备服务契约
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 定义模型准备服务契约

----------------------------------------------------------------*/

#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public interface IModelPreparationService
{
    Task<ModelPreparationStatus> GetStatusAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
    Task<ModelPreparationResult> DownloadAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
}
