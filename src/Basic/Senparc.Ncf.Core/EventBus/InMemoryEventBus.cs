/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InMemoryEventBus.cs
    File description: InMemoryEventBus related implementation

    Created by: Senparc - 20260215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260726
    Change description: v0.25.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

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
    /// Provides in-process integration-event publication and request/response correlation.
    /// </summary>
    public class InMemoryEventBus : IEventBus, IEventBusRequestClient
    {
        private readonly Channel<IIntegrationEvent> _channel;
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly TimeSpan _maxRequestTimeout;
        private readonly ConcurrentDictionary<Guid, PendingRequest> _pendingRequests = new();
        
        //Legacy comment translated to English.
        private readonly ConcurrentDictionary<Guid, DateTime> _processedEventIds = new();
        private readonly TimeSpan _eventIdRetentionPeriod = TimeSpan.FromMinutes(10);
        
        /// <summary>
        /// Initializes an in-memory event bus with bounded request timeout settings.
        /// </summary>
        /// <param name="logger">Optional logger for response and event diagnostics.</param>
        /// <param name="options">Optional event-bus options.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the configured request timeout is not finite and positive.</exception>
        public InMemoryEventBus(
            ILogger<InMemoryEventBus> logger = null,
            EventBusOptions options = null)
        {
            _logger = logger;
            _maxRequestTimeout = options?.MaxRequestTimeout ?? TimeSpan.FromMinutes(5);

            if (_maxRequestTimeout <= TimeSpan.Zero || _maxRequestTimeout == Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "EventBus MaxRequestTimeout must be a finite value greater than zero.");
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var channelOptions = new UnboundedChannelOptions
            {
                SingleReader = true,   //Legacy comment translated to English.
                SingleWriter = false   //Legacy comment translated to English.
            };
            _channel = Channel.CreateUnbounded<IIntegrationEvent>(channelOptions);
        }

        /// <summary>
        /// Publishes a request and waits for a response with the requested timeout and cancellation policy.
        /// </summary>
        /// <typeparam name="TResponse">The expected integration response type.</typeparam>
        /// <param name="request">The request event with a non-empty request identifier.</param>
        /// <param name="timeout">The maximum wait time, bounded by configured event-bus options.</param>
        /// <param name="cancellationToken">Token used to cancel publishing or waiting.</param>
        /// <returns>A task that completes with the correlated response.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when the request identifier is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is outside the allowed range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the request identifier is already pending.</exception>
        public async Task<TResponse> RequestAsync<TResponse>(
            IIntegrationRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TResponse : class, IIntegrationResponse
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.RequestId == Guid.Empty)
            {
                throw new ArgumentException("EventBus request ID cannot be empty.", nameof(request));
            }

            if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan || timeout > _maxRequestTimeout)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    timeout,
                    $"EventBus request timeout must be greater than zero and no longer than {_maxRequestTimeout}.");
            }

            var pendingRequest = new PendingRequest(typeof(TResponse));
            if (!_pendingRequests.TryAdd(request.RequestId, pendingRequest))
            {
                throw new InvalidOperationException(
                    $"An EventBus request with ID '{request.RequestId:N}' is already waiting for a response.");
            }

            try
            {
                await _channel.Writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);

                var response = await pendingRequest.Completion.Task
                    .WaitAsync(timeout, cancellationToken)
                    .ConfigureAwait(false);

                return (TResponse)response;
            }
            finally
            {
                _pendingRequests.TryRemove(request.RequestId, out _);
            }
        }

        /// <summary>
        /// Publishes an integration event to the in-memory event channel.
        /// </summary>
        /// <typeparam name="TEvent">The integration event type.</typeparam>
        /// <param name="event">The event to publish.</param>
        /// <param name="cancellationToken">Token used to cancel channel writing.</param>
        /// <returns>A value task representing channel publication.</returns>
        public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IIntegrationEvent
        {
            return _channel.Writer.WriteAsync(@event, cancellationToken);
        }
        
        /// <summary>
        /// Publishes a derived integration event while preserving parent metadata and rejecting circular chains.
        /// </summary>
        /// <typeparam name="TEvent">The derived integration event type.</typeparam>
        /// <param name="event">The derived event to publish.</param>
        /// <param name="parentEvent">The parent event whose metadata should be inherited.</param>
        /// <param name="cancellationToken">Token used to cancel channel writing.</param>
        /// <returns>A value task representing channel publication.</returns>
        /// <exception cref="ArgumentException">Thrown when either event is not an <see cref="IntegrationEvent"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when publication would create a circular event chain.</exception>
        public ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            //Legacy comment translated to English.
            if (parentEvent is not IntegrationEvent typedParent)
            {
                throw new ArgumentException("Parent event must inherit from IntegrationEvent base class", nameof(parentEvent));
            }
            
            if (@event is not IntegrationEvent typedEvent)
            {
                throw new ArgumentException("Event must inherit from IntegrationEvent base class", nameof(@event));
            }
            
            //Legacy comment translated to English.
            var metadata = typedParent.DeriveMetadata();
            
            //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        internal bool TryCompleteRequest(IIntegrationEvent @event)
        {
            if (@event is not IIntegrationResponse response || response.RequestId == Guid.Empty)
            {
                return false;
            }

            if (!_pendingRequests.TryGetValue(response.RequestId, out var pendingRequest))
            {
                return false;
            }

            if (!pendingRequest.ResponseType.IsInstanceOfType(response))
            {
                _logger?.LogWarning(
                    "Ignoring EventBus response {ResponseType} for request {RequestId}: expected {ExpectedResponseType}",
                    response.GetType().FullName,
                    response.RequestId,
                    pendingRequest.ResponseType.FullName);
                return false;
            }

            return pendingRequest.Completion.TrySetResult(response);
        }

        /// <summary>
        /// Marks an event identifier as processed for duplicate detection.
        /// </summary>
        /// <param name="eventId">The event identifier to mark.</param>
        /// <returns><see langword="true"/> when the identifier was new; otherwise, <see langword="false"/>.</returns>
        public bool TryMarkEventAsProcessed(Guid eventId)
        {
            //Legacy comment translated to English.
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

        //Legacy comment translated to English.
        internal ChannelReader<IIntegrationEvent> Reader => _channel.Reader;

        private sealed class PendingRequest
        {
            public PendingRequest(Type responseType)
            {
                ResponseType = responseType;
                Completion = new TaskCompletionSource<IIntegrationResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public Type ResponseType { get; }

            public TaskCompletionSource<IIntegrationResponse> Completion { get; }
        }
    }
}
