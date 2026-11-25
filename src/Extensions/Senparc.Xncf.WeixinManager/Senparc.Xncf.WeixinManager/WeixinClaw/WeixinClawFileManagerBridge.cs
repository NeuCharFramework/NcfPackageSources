/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawFileManagerBridge.cs
    File description: WeixinClawFileManagerBridge related definitions and implementation

    Created by: Senparc - 20261001

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.FileManager.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawFileManagerBridge
{
    private static readonly TimeSpan ImportTimeout = TimeSpan.FromSeconds(15);
    private readonly IEventBusRequestClient _requestClient;
    private readonly ILogger<WeixinClawFileManagerBridge> _logger;

    /// <summary>
    /// Initializes the event-bus bridge used to import received media into FileManager.
    /// </summary>
    /// <param name="requestClient">Event-bus request client for FileManager import requests.</param>
    /// <param name="logger">Bridge logger.</param>
    public WeixinClawFileManagerBridge(
        IEventBusRequestClient requestClient,
        ILogger<WeixinClawFileManagerBridge> logger)
    {
        _requestClient = requestClient;
        _logger = logger;
    }

    /// <summary>
    /// Requests FileManager import for inbound media and records non-cancellation failures on the media object.
    /// </summary>
    /// <param name="tenantId">The tenant receiving the media.</param>
    /// <param name="accountId">The Weixin account identifier.</param>
    /// <param name="messageId">The inbound message identifier.</param>
    /// <param name="media">The stored media metadata to update.</param>
    /// <param name="content">The media bytes sent to FileManager.</param>
    /// <param name="cancellationToken">Token used to cancel the import request.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="media"/> or <paramref name="content"/> is <see langword="null"/>.</exception>
    public async Task ImportAsync(
        int tenantId,
        int accountId,
        string messageId,
        WeixinClawStoredMedia media,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            var response = await _requestClient.RequestAsync(
                new FileManagerImportRequestedEvent(
                    tenantId,
                    accountId,
                    messageId,
                    media.StorageKey,
                    content,
                    media.Name,
                    media.ContentType,
                    $"WeixinClaw AccountId={accountId}, MessageId={messageId}"),
                ImportTimeout,
                cancellationToken).ConfigureAwait(false);
            if (!response.Success || response.Result == null)
            {
                media.FileManagerError = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "FileManager 未能完成文件导入。"
                    : response.ErrorMessage;
                _logger.LogWarning(
                    "微信附件导入 FileManager 失败：TenantId={TenantId}, AccountId={AccountId}, MessageId={MessageId}, FileName={FileName}, Error={Error}",
                    tenantId,
                    accountId,
                    messageId,
                    media.Name,
                    media.FileManagerError);
                return;
            }

            media.FileManagerFileId = response.Result.FileId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            media.FileManagerError = ex.Message;
            _logger.LogWarning(
                ex,
                "微信附件请求 FileManager 导入失败：TenantId={TenantId}, AccountId={AccountId}, MessageId={MessageId}, FileName={FileName}",
                tenantId,
                accountId,
                messageId,
                media.Name);
        }
    }
}
