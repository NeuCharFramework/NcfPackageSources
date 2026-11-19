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

    public NcfFileManagerGateway(
        NcfFileService fileService,
        IServiceProvider serviceProvider)
    {
        _fileService = fileService;
        _registerManager = new XncfRegisterManager(serviceProvider);
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _registerManager.CheckXncfAvailable(new Register());
    }

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
