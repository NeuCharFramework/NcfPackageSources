/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FileManagerImportCompletedEventHandler.cs
    文件功能描述：FileManagerImportCompletedEventHandler 相关定义与实现


    创建标识：Senparc - 20261002

    修改标识：Senparc - 20261103
    修改描述：v0.24.16 add comprehensive tests for MCP endpoint management and background tenant scope

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.WeixinManager.Domain.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Application.EventHandlers;

public sealed class FileManagerImportCompletedEventHandler
    : IIntegrationEventHandler<FileManagerImportCompletedEvent>
{
    private readonly WeixinClawMessageRecordService _recordService;
    private readonly ILogger<FileManagerImportCompletedEventHandler> _logger;

    public FileManagerImportCompletedEventHandler(
        WeixinClawMessageRecordService recordService,
        ILogger<FileManagerImportCompletedEventHandler> logger)
    {
        _recordService = recordService;
        _logger = logger;
    }

    public async Task Handle(
        FileManagerImportCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        if (@event.AccountId <= 0
            || string.IsNullOrWhiteSpace(@event.MessageId)
            || string.IsNullOrWhiteSpace(@event.StorageKey))
        {
            return;
        }

        if (SiteConfig.SenparcCoreSetting.EnableMultiTenant)
        {
            if (@event.TenantId <= 0)
            {
                _logger.LogWarning(
                    "收到缺少有效 TenantId 的 FileManager 导入完成事件，跳过消息回填：RequestId={RequestId}, AccountId={AccountId}, MessageId={MessageId}",
                    @event.RequestId,
                    @event.AccountId,
                    @event.MessageId);
                return;
            }

            _recordService.SetTenantInfo(CreateTenantInfo(@event.TenantId));
        }

        var updated = await _recordService.TryUpdateInboundMediaImportAsync(
            @event.AccountId,
            @event.MessageId,
            @event.StorageKey,
            @event.Result?.FileId,
            @event.Success ? null : @event.ErrorMessage).ConfigureAwait(false);
        if (updated)
        {
            _logger.LogInformation(
                "已回填微信附件 FileManager 导入结果：RequestId={RequestId}, TenantId={TenantId}, AccountId={AccountId}, MessageId={MessageId}, StorageKey={StorageKey}, Success={Success}, FileId={FileId}",
                @event.RequestId,
                @event.TenantId,
                @event.AccountId,
                @event.MessageId,
                @event.StorageKey,
                @event.Success,
                @event.Result?.FileId);
        }
        else
        {
            _logger.LogDebug(
                "FileManager 导入完成事件未匹配到可回填的微信消息记录：RequestId={RequestId}, TenantId={TenantId}, AccountId={AccountId}, MessageId={MessageId}, StorageKey={StorageKey}, Success={Success}",
                @event.RequestId,
                @event.TenantId,
                @event.AccountId,
                @event.MessageId,
                @event.StorageKey,
                @event.Success);
        }
    }

    private static RequestTenantInfo CreateTenantInfo(int tenantId)
    {
        var tenantInfo = new RequestTenantInfo { Id = tenantId };
        tenantInfo.TryMatch(tenantId > 0);
        return tenantInfo;
    }
}
