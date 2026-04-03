using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    ///Event bus publishing interface
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publish events asynchronously (non-blocking, write to Channel and return)
        /// </summary>
        ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
            
        /// <summary>
        /// Publish derived events asynchronously (automatically inherit the chain information of the parent event to prevent circular references)
        /// </summary>
        ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}