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
        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Senparc.Xncf.FileManager 尚未安装或未开放。");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var scope = request.ResourceKind switch
        {
            FileManagerResourceKind.KnowledgeBase => NcfFileResourceScope.KnowledgeBase,
            FileManagerResourceKind.SiteAsset => NcfFileResourceScope.SiteAsset,
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
