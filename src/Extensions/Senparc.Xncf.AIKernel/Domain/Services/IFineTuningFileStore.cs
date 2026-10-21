/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IFineTuningFileStore.cs
    文件功能描述：微调文件存储契约
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 定义微调文件存储契约

----------------------------------------------------------------*/

using Senparc.Xncf.FileManager.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public interface IFineTuningFileStore
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        Task<FileManagerImportResult> ImportAsync(
            Stream content,
            string fileName,
            string contentType,
            long length,
            string description,
            CancellationToken cancellationToken = default);
    }
}
