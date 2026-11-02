/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawFileManagerBridge.cs
    文件功能描述：WeixinClawFileManagerBridge 相关定义与实现


    创建标识：Senparc - 20261001

    修改标识：Senparc - 20261103
    修改描述：v0.24.16 完善微信 Claw 消息收发、回执完整性与绑定配置交互

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

    public WeixinClawFileManagerBridge(
        IEventBusRequestClient requestClient,
        ILogger<WeixinClawFileManagerBridge> logger)
    {
        _requestClient = requestClient;
        _logger = logger;
    }

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
