/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModulesInventoryResponseHandler.cs
    File description: XncfModulesInventoryResponseHandler related implementation

    Created by: Senparc - 20260510

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

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
    /// Completes the pending XNCF module inventory waiter when a response event arrives.
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

        /// <inheritdoc />
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
