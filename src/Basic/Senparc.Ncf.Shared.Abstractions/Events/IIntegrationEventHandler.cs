/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIntegrationEventHandler.cs
    File description: IIntegrationEventHandler related implementation

    Created by: Senparc - 20260216

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Handles one integration-event type.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The event type accepted by the handler.</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent>
        where TIntegrationEvent : IIntegrationEvent
    {
        /// <summary>
        /// Processes an event and returns when handling has completed.
        /// </summary>
        /// <param name="event">The event to handle.</param>
        /// <param name="cancellationToken">Token used to cancel handling.</param>
        Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
    }
}
