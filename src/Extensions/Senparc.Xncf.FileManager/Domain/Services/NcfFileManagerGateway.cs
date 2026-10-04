/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfFileManagerGateway.cs
    文件功能描述：NcfFileManagerGateway.cs implementation and project behavior.


    创建标识：Senparc - 20260930

    修改标识：Senparc - 20261005
    修改描述：v0.7.3 0.7.3 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

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
            _ => throw new ArgumentOutOfRangeException(nameof(request.ResourceKind))
        };
        var file = await _fileService.UploadStreamAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            request.Length,
            scope,
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
}
