/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FineTuningFileStore.cs
    文件功能描述：保存微调数据集与产物文件
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 保存微调数据集与产物文件

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.FileManager.Abstractions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public class FineTuningFileStore : IFineTuningFileStore
    {
        private readonly IServiceProvider _serviceProvider;

        public FineTuningFileStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            GetGateway().IsAvailableAsync(cancellationToken);

        public Task<FileManagerImportResult> ImportAsync(
            Stream content,
            string fileName,
            string contentType,
            long length,
            string description,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("A file name is required.", nameof(fileName));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return GetGateway().ImportAsync(new FileManagerImportRequest(
                content, fileName, contentType, length, description, FileManagerResourceKind.FineTuning),
                cancellationToken);
        }

        private IFileManagerGateway GetGateway() =>
            _serviceProvider.GetService<IFileManagerGateway>()
            ?? throw new InvalidOperationException("FileManager is not available. Install and enable Senparc.Xncf.FileManager before using fine-tuning file storage.");
    }
}
