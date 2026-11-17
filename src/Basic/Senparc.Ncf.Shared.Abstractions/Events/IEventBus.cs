/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: IEventBus.cs
    File description: IEventBus related implementation
    
    
    Created by: Senparc - 20260216
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
            
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}