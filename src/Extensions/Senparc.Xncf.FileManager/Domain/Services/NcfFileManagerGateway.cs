/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFileManagerGateway.cs
    File description: NcfFileManagerGateway.cs implementation and project behavior.

    Created by: Senparc - 20260930

    Modified by: Senparc - 20261005
    Change description: v0.7.3 0.7.3 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261021
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services;

public sealed class NcfFileManagerGateway : IFileManagerGateway
{
    private const long MaxFineTuningImportBytes = 512L * 1024 * 1024;

    private readonly NcfFileService _fileService;
    private readonly XncfRegisterManager _registerManager;

    /// <summary>
    /// Initializes the FileManager gateway used by cross-module file consumers.
    /// </summary>
    /// <param name="fileService">The FileManager service used for upload, read, and delete operations.</param>
    /// <param name="serviceProvider">The provider used to verify module availability.</param>
    public NcfFileManagerGateway(
        NcfFileService fileService,
        IServiceProvider serviceProvider)
    {
        _fileService = fileService;
        _registerManager = new XncfRegisterManager(serviceProvider);
    }

    /// <summary>
    /// Determines whether the FileManager XNCF is installed and available.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel availability checking.</param>
    /// <returns>A task returning <see langword="true"/> when the module is available.</returns>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _registerManager.CheckXncfAvailable(new Register());
    }

    /// <summary>
    /// Imports a stream into the scope mapped from a cross-module resource kind.
    /// </summary>
    /// <param name="request">The stream, file metadata, resource kind, and optional description.</param>
    /// <param name="cancellationToken">Token used to cancel availability or upload work.</param>
    /// <returns>The persisted file reference returned to the calling module.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when FileManager is unavailable.</exception>
    public async Task<FileManagerImportResult> ImportAsync(
        FileManagerImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Senparc.Xncf.FileManager 尚未安装或未开放。");
        }

        var scope = request.ResourceKind switch
        {
            FileManagerResourceKind.KnowledgeBase => NcfFileResourceScope.KnowledgeBase,
            FileManagerResourceKind.SiteAsset => NcfFileResourceScope.SiteAsset,
            FileManagerResourceKind.PrivateAttachment => NcfFileResourceScope.PrivateAttachment,
            // FileManager currently has no dedicated fine-tuning scope. Keep the
            // abstraction-level kind while using its private, non-public storage policy.
            FileManagerResourceKind.FineTuning => NcfFileResourceScope.PrivateAttachment,
            FileManagerResourceKind.Skill => NcfFileResourceScope.Skill,
            _ => throw new ArgumentOutOfRangeException(nameof(request.ResourceKind))
        };
        long? maxFileSizeBytes = request.ResourceKind == FileManagerResourceKind.FineTuning
            ? MaxFineTuningImportBytes
            : null;
        var file = await _fileService.UploadStreamAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            request.Length,
            scope,
            cancellationToken: cancellationToken,
            maxFileSizeBytes: maxFileSizeBytes).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            await _fileService.UpdateFileNoteAsync(file.Id, request.Description).ConfigureAwait(false);
        }

        return new FileManagerImportResult(
            file.Id,
            file.FileName,
            file.ContentType,
            file.FileSize,
            request.ResourceKind.ToString());
    }

    /// <summary>
    /// Opens a FileManager file for cross-module streaming.
    /// </summary>
    /// <param name="fileId">The FileManager file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the read request.</param>
    /// <returns>The file metadata and readable stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fileId"/> is not positive.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public async Task<FileManagerReadResult> OpenReadAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId));
        }

        var result = await _fileService.OpenReadAsync(fileId).ConfigureAwait(false);
        if (result == null)
        {
            throw new FileNotFoundException($"FileManager file does not exist: {fileId}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new FileManagerReadResult(
            result.File.Id,
            result.File.FileName,
            result.File.ContentType,
            result.File.FileSize,
            result.Stream);
    }

    /// <summary>
    /// Deletes a FileManager file by identifier.
    /// </summary>
    /// <param name="fileId">The FileManager file identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the delete request.</param>
    /// <returns>A task that completes after deletion.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fileId"/> is not positive.</exception>
    public async Task DeleteAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        if (fileId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _fileService.DeleteFileAsync(fileId).ConfigureAwait(false);
    }
}
