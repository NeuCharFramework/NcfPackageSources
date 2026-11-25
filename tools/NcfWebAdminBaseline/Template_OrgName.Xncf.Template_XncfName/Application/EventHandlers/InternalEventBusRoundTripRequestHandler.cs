/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InternalEventBusRoundTripRequestHandler.cs
    File description: template internal EventBus round-trip request handler

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Template_OrgName.Xncf.Template_XncfName.Application.Events;

namespace Template_OrgName.Xncf.Template_XncfName.Application.EventHandlers
{
    public sealed class InternalEventBusRoundTripRequestHandler
        : IIntegrationEventHandler<InternalEventBusRoundTripRequest>
    {
        private readonly IEventBus _eventBus;

        public InternalEventBusRoundTripRequestHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task Handle(
            InternalEventBusRoundTripRequest @event,
            CancellationToken cancellationToken)
        {
            var response = new InternalEventBusRoundTripResponse(
                @event.RequestId,
                @event.SentAtUtc,
                DateTime.UtcNow);

            await _eventBus.PublishDerivedAsync(response, @event, cancellationToken);
        }
    }
}
