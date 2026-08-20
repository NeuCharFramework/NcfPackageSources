/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModulesInventoryResponseHandler.cs
    文件功能描述：XncfModulesInventoryResponseHandler 相关实现
    
    
    创建标识：Senparc - 20260510
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.XncfBuilder.Abstractions;
using Senparc.Xncf.XncfBuilder.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Application.EventHandlers
{
    /// <summary>
    /// 完成 <see cref="XncfModulesInventoryResponseEvent"/> 与等待方的关联。
    /// </summary>
    public class XncfModulesInventoryResponseHandler : IIntegrationEventHandler<XncfModulesInventoryResponseEvent>
    {
        private readonly IXncfModulesInventoryRequestWaiter _waiter;
        private readonly ILogger<XncfModulesInventoryResponseHandler> _logger;

        public XncfModulesInventoryResponseHandler(
            IXncfModulesInventoryRequestWaiter waiter,
            ILogger<XncfModulesInventoryResponseHandler> logger)
        {
            _waiter = waiter;
            _logger = logger;
        }

        public Task Handle(XncfModulesInventoryResponseEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "XncfModulesInventoryResponseHandler: RequestId={RequestId}, Success={Success}",
                @event.RequestId,
                @event.Success);

            _waiter.TrySetResult(
                @event.RequestId,
                @event.Success,
                @event.Message,
                @event.InstalledModules,
                @event.NotInstalledModules);
            return Task.CompletedTask;
        }
    }
}
