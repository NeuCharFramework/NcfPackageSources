/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningFileStore.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

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

        /// <summary>
        /// Initializes the FileManager-backed fine-tuning file store.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve FileManager gateway services.</param>
        public FineTuningFileStore(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Determines whether FileManager is available for fine-tuning storage.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel availability checking.</param>
        /// <returns>A task returning the module availability.</returns>
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            GetGateway().IsAvailableAsync(cancellationToken);

        /// <summary>
        /// Imports a fine-tuning file into FileManager.
        /// </summary>
        /// <param name="content">The readable file stream.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="contentType">The MIME type.</param>
        /// <param name="length">The declared file length.</param>
        /// <param name="description">The optional description.</param>
        /// <param name="cancellationToken">Token used to cancel import.</param>
        /// <returns>The imported FileManager reference.</returns>
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

        /// <summary>
        /// Opens one fine-tuning file through FileManager.
        /// </summary>
        /// <param name="fileId">The FileManager file identifier.</param>
        /// <param name="cancellationToken">Token used to cancel reading.</param>
        /// <returns>The file metadata and readable stream.</returns>
        public Task<FileManagerReadResult> OpenReadAsync(
            int fileId,
            CancellationToken cancellationToken = default) =>
            GetGateway().OpenReadAsync(fileId, cancellationToken);

        /// <summary>
        /// Deletes one fine-tuning file through FileManager.
        /// </summary>
        /// <param name="fileId">The FileManager file identifier.</param>
        /// <param name="cancellationToken">Token used to cancel deletion.</param>
        /// <returns>A task that completes after deletion.</returns>
        public Task DeleteAsync(
            int fileId,
            CancellationToken cancellationToken = default) =>
            GetGateway().DeleteAsync(fileId, cancellationToken);

        /// <summary>
        /// Resolves the FileManager gateway or reports that FileManager is unavailable.
        /// </summary>
        /// <returns>The registered FileManager gateway.</returns>
        /// <exception cref="InvalidOperationException">Thrown when FileManager is not registered.</exception>
        private IFileManagerGateway GetGateway() =>
            _serviceProvider.GetService<IFileManagerGateway>()
            ?? throw new InvalidOperationException("FileManager is not available. Install and enable Senparc.Xncf.FileManager before using fine-tuning file storage.");
    }
}
