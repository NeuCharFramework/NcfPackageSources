using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Senparc.Ncf.Shared.Abstractions.Events;
using System.Collections.Concurrent;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Senparc.Ncf.Core.EventBus
{
    /// <summary>
    /// In-memory Channel-based event bus implementation
    /// </summary>
    public class InMemoryEventBus : IEventBus
    {
        private readonly Channel<IIntegrationEvent> _channel;
        private readonly ILogger<InMemoryEventBus> _logger;
        
        // Tracks processed event IDs to prevent duplicate processing (sliding window of last 10 minutes)
        private readonly ConcurrentDictionary<Guid, DateTime> _processedEventIds = new();
        private readonly TimeSpan _eventIdRetentionPeriod = TimeSpan.FromMinutes(10);
        
        public InMemoryEventBus(ILogger<InMemoryEventBus> logger = null)
        {
            _logger = logger;
            
            // Configure unbounded channel (memory can grow when producer speed > consumer speed, but producers won't block)
            // Use Channel.CreateBounded if backpressure control is required
            var options = new UnboundedChannelOptions
            {
                SingleReader = false,  // Support concurrent reads from multiple consumers
                SingleWriter = false   // Allow concurrent writes from multiple business modules
            };
            _channel = Channel.CreateUnbounded<IIntegrationEvent>(options);
        }

        public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IIntegrationEvent
        {
            return _channel.Writer.WriteAsync(@event, cancellationToken);
        }
        
        /// <summary>
        /// Publish a derived event (automatically inherit parent chain metadata and detect cycles)
        /// Note: this method requires event types to inherit from the IntegrationEvent base class
        /// </summary>
        public ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            // Only IntegrationEvent base class is supported (DeriveMetadata and related methods are required)
            if (parentEvent is not IntegrationEvent typedParent)
            {
                throw new ArgumentException("Parent event must inherit from IntegrationEvent base class", nameof(parentEvent));
            }
            
            if (@event is not IntegrationEvent typedEvent)
            {
                throw new ArgumentException("Event must inherit from IntegrationEvent base class", nameof(@event));
            }
            
            // Metadata for the derived event
            var metadata = typedParent.DeriveMetadata();
            
            // Pre-check for circular references before publishing
            var newEventType = typedEvent.GetType().Name;
            if (typedParent.HasCircularReference(newEventType))
            {
                _logger?.LogError(
                    "Circular reference detected before publishing: {EventType} would create cycle in chain: {Chain}→{NewType}",
                    newEventType,
                    typedParent.EventChain,
                    newEventType);
                    
                throw new InvalidOperationException(
                    $"Circular reference detected: Event chain '{typedParent.EventChain}→{newEventType}' contains duplicate event types. " +
                    $"This would cause an infinite event loop.");
            }
            
            // Create a new event instance with propagated chain metadata
            var derivedEvent = typedEvent with
            {
                ParentEventId = metadata.ParentEventId,
                Depth = metadata.Depth,
                EventChain = metadata.EventChain
            };
            
            _logger?.LogDebug(
                "Publishing derived event: {EventType} (ParentId: {ParentId}, Depth: {Depth}, Chain: {Chain})",
                newEventType,
                metadata.ParentEventId,
                metadata.Depth,
                metadata.EventChain);
            
            return _channel.Writer.WriteAsync(derivedEvent, cancellationToken);
        }

        /// <summary>
        /// Check whether the event has already been processed (to prevent duplicate handling)
        /// </summary>
        public bool TryMarkEventAsProcessed(Guid eventId)
        {
            // Clean up expired event IDs (once per 100 calls)
            if (_processedEventIds.Count > 0 && _processedEventIds.Count % 100 == 0)
            {
                CleanupExpiredEventIds();
            }

            return _processedEventIds.TryAdd(eventId, DateTime.UtcNow);
        }

        private void CleanupExpiredEventIds()
        {
            var cutoffTime = DateTime.UtcNow.Subtract(_eventIdRetentionPeriod);
            var expiredKeys = _processedEventIds
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _processedEventIds.TryRemove(key, out _);
            }
        }

        // Exposed for HostedService reads within the same assembly
        internal ChannelReader<IIntegrationEvent> Reader => _channel.Reader;
    }
}