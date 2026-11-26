/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InternalEventBusRoundTripRequestHandler.cs
    File description: template internal EventBus round-trip request handler

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

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

        /// <summary>
        /// Initializes the event-bus handler used by the round-trip sample.
        /// </summary>
        /// <param name="eventBus">The event bus used to publish the response event.</param>
        public InternalEventBusRoundTripRequestHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// Handles a round-trip request by publishing a response that preserves the request
        /// identifier and increments the event depth for diagnostic output.
        /// </summary>
        /// <param name="event">The incoming round-trip request.</param>
        /// <param name="cancellationToken">Cancellation requested by the event-bus dispatcher.</param>
        /// <returns>A task that completes after the response event is published.</returns>
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
