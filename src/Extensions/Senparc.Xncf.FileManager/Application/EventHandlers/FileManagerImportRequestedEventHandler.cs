/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileManagerImportRequestedEventHandler.cs
    File description: FileManagerImportRequestedEventHandler related definitions and implementation

    Created by: Senparc - 20261001

    Modified by: Senparc - 20261103
    Change description: v0.8.1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Application.EventHandlers;

public sealed class FileManagerImportRequestedEventHandler
    : IIntegrationEventHandler<FileManagerImportRequestedEvent>
{
    private readonly IFileManagerGateway _gateway;
    private readonly NcfFileService _fileService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<FileManagerImportRequestedEventHandler> _logger;

    /// <summary>
    /// Initializes the integration-event handler for cross-module file imports.
    /// </summary>
    /// <param name="gateway">The gateway used to persist imported files.</param>
    /// <param name="fileService">The FileManager service used to apply tenant context.</param>
    /// <param name="eventBus">The event bus used to publish completion events.</param>
    /// <param name="logger">The logger used for import failures.</param>
    public FileManagerImportRequestedEventHandler(
        IFileManagerGateway gateway,
        NcfFileService fileService,
        IEventBus eventBus,
        ILogger<FileManagerImportRequestedEventHandler> logger)
    {
        _gateway = gateway;
        _fileService = fileService;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Imports event content under the requested tenant and publishes a completion event.
    /// </summary>
    /// <param name="@event">The import request event and file metadata.</param>
    /// <param name="cancellationToken">Token used to cancel the import.</param>
    /// <returns>A task that completes after the completion event is published.</returns>
    /// <exception cref="OperationCanceledException">Propagated when the import is cancelled.</exception>
    public async Task Handle(
        FileManagerImportRequestedEvent @event,
        CancellationToken cancellationToken)
    {
        FileManagerImportCompletedEvent response;
        try
        {
            if (@event.Content == null || @event.Content.Length == 0)
            {
                throw new ArgumentException("导入文件内容不能为空。", nameof(@event));
            }

            if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
            {
                if (@event.TenantId <= 0)
                {
                    throw new InvalidOperationException("多租户模式下导入文件必须指定有效租户。");
                }

                _fileService.SetTenantInfo(new RequestTenantInfo
                {
                    Id = @event.TenantId
                });
            }

            await using var stream = new MemoryStream(@event.Content, writable: false);
            var result = await _gateway.ImportAsync(
                new FileManagerImportRequest(
                    stream,
                    @event.FileName,
                    @event.ContentType,
                    @event.Content.LongLength,
                    @event.Description,
                    @event.ResourceKind),
                cancellationToken).ConfigureAwait(false);
            response = new FileManagerImportCompletedEvent(
                @event.RequestId,
                @event.TenantId,
                @event.AccountId,
                @event.MessageId,
                @event.StorageKey,
                true,
                null,
                result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "处理 FileManager 导入请求失败：RequestId={RequestId}, TenantId={TenantId}, FileName={FileName}",
                @event.RequestId,
                @event.TenantId,
                @event.FileName);
            response = new FileManagerImportCompletedEvent(
                @event.RequestId,
                @event.TenantId,
                @event.AccountId,
                @event.MessageId,
                @event.StorageKey,
                false,
                ex.Message,
                null);
        }

        await _eventBus.PublishDerivedAsync(response, @event, cancellationToken)
            .ConfigureAwait(false);
    }
}
