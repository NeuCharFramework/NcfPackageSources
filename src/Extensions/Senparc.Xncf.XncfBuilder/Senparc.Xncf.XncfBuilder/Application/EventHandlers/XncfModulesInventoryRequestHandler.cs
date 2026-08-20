/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModulesInventoryRequestHandler.cs
    文件功能描述：XncfModulesInventoryRequestHandler 相关实现
    
    
    创建标识：Senparc - 20260510
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.XncfBuilder.Abstractions.Events;
using Senparc.Xncf.XncfModuleManager.Domain.Services;

namespace Senparc.Xncf.XncfBuilder.Application.EventHandlers
{
    /// <summary>
    /// 处理 <see cref="XncfModulesInventoryRequestEvent"/>：汇总已安装（库版本与 Register 一致）与未安装/未对齐的模块。
    /// </summary>
    public class XncfModulesInventoryRequestHandler : IIntegrationEventHandler<XncfModulesInventoryRequestEvent>
    {
        private readonly XncfModuleServiceExtension _xncfModuleServiceExtension;
        private readonly IEventBus _eventBus;
        private readonly ILogger<XncfModulesInventoryRequestHandler> _logger;

        public XncfModulesInventoryRequestHandler(
            XncfModuleServiceExtension xncfModuleServiceExtension,
            IEventBus eventBus,
            ILogger<XncfModulesInventoryRequestHandler> logger)
        {
            _xncfModuleServiceExtension = xncfModuleServiceExtension;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(XncfModulesInventoryRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("XncfModulesInventoryRequestHandler: RequestId={RequestId}", @event.RequestId);

            try
            {
                var dbModules = await _xncfModuleServiceExtension.GetFullListAsync(z => true).ConfigureAwait(false);
                var registers = XncfRegisterManager.RegisterList
                    .Where(z => !z.IgnoreInstall)
                    .OrderBy(z => z.MenuName)
                    .ToList();

                var installed = new List<XncfModuleInventoryItem>();
                var notInstalled = new List<XncfModuleInventoryItem>();

                foreach (var reg in registers)
                {
                    var mod = dbModules.FirstOrDefault(m => m.Uid == reg.Uid);
                    if (mod != null && string.Equals(mod.Version, reg.Version, StringComparison.Ordinal))
                    {
                        installed.Add(new XncfModuleInventoryItem(
                            reg.MenuName,
                            reg.Name,
                            reg.Uid,
                            reg.Version,
                            mod.Version,
                            mod.State.ToString()));
                    }
                    else
                    {
                        var stateDesc = mod == null
                            ? "未安装"
                            : $"未对齐（库版本 {mod.Version}，程序集版本 {reg.Version}）";
                        notInstalled.Add(new XncfModuleInventoryItem(
                            reg.MenuName,
                            reg.Name,
                            reg.Uid,
                            reg.Version,
                            mod?.Version,
                            stateDesc));
                    }
                }

                var response = new XncfModulesInventoryResponseEvent(
                    @event.RequestId,
                    true,
                    $"已扫描 {registers.Count} 个可安装模块：已安装当前版本 {installed.Count} 个，其余 {notInstalled.Count} 个。",
                    installed.ToArray(),
                    notInstalled.ToArray());

                await _eventBus.PublishDerivedAsync(response, @event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XncfModulesInventoryRequestHandler failed");
                var response = new XncfModulesInventoryResponseEvent(
                    @event.RequestId,
                    false,
                    ex.Message,
                    Array.Empty<XncfModuleInventoryItem>(),
                    Array.Empty<XncfModuleInventoryItem>());
                await _eventBus.PublishDerivedAsync(response, @event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
