/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IFineTuningFileStore.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

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

        Task<FileManagerReadResult> OpenReadAsync(
            int fileId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            int fileId,
            CancellationToken cancellationToken = default);
    }
}
