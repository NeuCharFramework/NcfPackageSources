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
