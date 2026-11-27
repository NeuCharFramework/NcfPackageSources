/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IEventBus.cs
    File description: IEventBus related implementation

    Created by: Senparc - 20260216

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.7.1.2-preview3 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Publishes integration events to handlers registered in the current application.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to all compatible handlers.
        /// </summary>
        /// <typeparam name="TEvent">The concrete integration-event type.</typeparam>
        /// <param name="event">The event instance to publish.</param>
        /// <param name="cancellationToken">Token used to cancel handler dispatch.</param>
        ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
            
        /// <summary>
        /// Publishes an event while preserving the parent event metadata used for
        /// correlation and circular-dispatch detection.
        /// </summary>
        /// <typeparam name="TEvent">The concrete derived integration-event type.</typeparam>
        /// <param name="event">The event instance to publish.</param>
        /// <param name="parentEvent">The event that caused this derived event.</param>
        /// <param name="cancellationToken">Token used to cancel handler dispatch.</param>
        ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}
