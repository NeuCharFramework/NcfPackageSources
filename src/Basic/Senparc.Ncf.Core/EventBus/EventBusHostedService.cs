/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EventBusHostedService.cs
    File description: EventBusHostedService related implementation

    Created by: Senparc - 20260215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260726
    Change description: v0.25.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.EventBus;
using System;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.Events;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Core.EventBus
{
    /// <summary>
    /// Consumes integration events from the in-memory event bus, enforces configured
    /// duplicate/depth/cycle limits, and dispatches handlers with bounded concurrency.
    /// </summary>
    public class EventBusHostedService : BackgroundService
    {
        private readonly InMemoryEventBus _eventBus;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBusHostedService> _logger;
        private readonly EventBusOptions _options;

        /// <summary>
        /// Initializes the hosted event-bus consumer.
        /// </summary>
        /// <param name="eventBus">The in-memory event bus whose reader supplies events.</param>
        /// <param name="serviceProvider">The provider used to resolve event handlers in a scoped lifetime.</param>
        /// <param name="logger">The logger used for lifecycle, dispatch, retry, and failure diagnostics.</param>
        /// <param name="options">Optional dispatch limits and retry behavior.</param>
        public EventBusHostedService(
            InMemoryEventBus eventBus,
            IServiceProvider serviceProvider,
            ILogger<EventBusHostedService> logger,
            EventBusOptions options = null)
        {
            _eventBus = eventBus;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options ?? new EventBusOptions();
        }

        /// <summary>
        /// Reads events until cancellation, validates event-chain safeguards, and dispatches
        /// accepted events using the configured concurrency and retry policy.
        /// </summary>
        /// <param name="stoppingToken">The token used to stop reading and dispatching events.</param>
        /// <returns>A task that completes when the event stream is exhausted or cancellation is requested.</returns>
        /// <exception cref="OperationCanceledException">Observed internally to finish graceful shutdown.</exception>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Senparc NCF EventBus Service is starting with MaxConcurrency={MaxConcurrency}, EnableDuplicateDetection={EnableDuplicateDetection}, MaxEventChainDepth={MaxEventChainDepth}, EnableCircularReferenceDetection={EnableCircularReferenceDetection}", 
                _options.MaxConcurrency, 
                _options.EnableDuplicateDetection,
                _options.MaxEventChainDepth,
                _options.EnableCircularReferenceDetection);

            // The semaphore limits handler work while the reader continues to accept queued events.
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var activeTasks = new List<Task>();

            try
            {
                await foreach (var @event in _eventBus.Reader.ReadAllAsync(stoppingToken))
                {
                    if (_options.EnableDuplicateDetection)
                    {
                        if (!_eventBus.TryMarkEventAsProcessed(@event.Id))
                        {
                            _logger.LogWarning(
                                "Duplicate event detected and skipped: {EventType} (Id: {EventId})",
                                @event.GetType().Name,
                                @event.Id);
                            continue;
                        }
                    }
                    
                    if (@event.Depth >= _options.MaxEventChainDepth)
                    {
                        _logger.LogError(
                            "Event chain depth limit exceeded: {EventType} (Id: {EventId}, Depth: {Depth}, Chain: {Chain})",
                            @event.GetType().Name,
                            @event.Id,
                            @event.Depth,
                            @event.EventChain);
                        continue;
                    }
                    
                    if (_options.EnableCircularReferenceDetection)
                    {
                        var currentEventType = @event.GetType().Name;
                        if (!string.IsNullOrEmpty(@event.EventChain) && 
                            @event.EventChain.Contains(currentEventType))
                        {
                            _logger.LogError(
                                "Circular reference detected: {EventType} (Id: {EventId}, Chain: {Chain}→{CurrentType})",
                                @event.GetType().Name,
                                @event.Id,
                                @event.EventChain,
                                currentEventType);
                            continue;
                        }
                    }

                    await semaphore.WaitAsync(stoppingToken);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessEventAsync(@event, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex, 
                                "Error processing event {EventType} (Id: {EventId}, Depth: {Depth})", 
                                @event.GetType().Name, 
                                @event.Id,
                                @event.Depth);
                            
                            if (_options.RetryOnFailure && _options.MaxRetryAttempts > 0)
                            {
                                await RetryEventProcessingAsync(@event, stoppingToken);
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, stoppingToken);

                    activeTasks.Add(task);

                    if (activeTasks.Count >= _options.MaxConcurrency * 2)
                    {
                        activeTasks.RemoveAll(t => t.IsCompleted);
                    }
                }

                await Task.WhenAll(activeTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("EventBus service is stopping gracefully...");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error in EventBus service");
                throw;
            }
        }

        /// <summary>
        /// Resolves all handlers for the concrete event type and invokes them in registration order.
        /// A pending request is completed before handler dispatch so response events can satisfy
        /// request callers even when no additional handler is registered.
        /// </summary>
        /// <param name="@event">The integration event to dispatch.</param>
        /// <param name="ct">The cancellation token passed to each handler.</param>
        /// <returns>A task that completes after all resolved handlers finish.</returns>
        /// <exception cref="Exception">Propagates a handler failure after logging it.</exception>
        private async Task ProcessEventAsync(IIntegrationEvent @event, CancellationToken ct)
        {
            var startTime = DateTime.UtcNow;

            var completedPendingRequest = _eventBus.TryCompleteRequest(@event);
            
            using var scope = _serviceProvider.CreateScope();

            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType).ToList();

            if (!handlers.Any())
            {
                if (completedPendingRequest)
                {
                    _logger.LogDebug(
                        "EventBus response {EventType} completed a pending request without additional handlers (Id: {EventId}, Depth: {Depth})",
                        @event.GetType().Name,
                        @event.Id,
                        @event.Depth);
                }
                else
                {
                    _logger.LogWarning(
                        "No handler found for event {EventType} (Id: {EventId}, Depth: {Depth})",
                        @event.GetType().Name,
                        @event.Id,
                        @event.Depth);
                }
                return;
            }

            _logger.LogDebug(
                "Processing event {EventType} (Id: {EventId}, Depth: {Depth}, Chain: {Chain}) with {HandlerCount} handler(s)",
                @event.GetType().Name,
                @event.Id,
                @event.Depth,
                @event.EventChain,
                handlers.Count);

            foreach (var handler in handlers)
            {
                var handlerStartTime = DateTime.UtcNow;
                try
                {
                    var method = handler.GetType().GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle));
                    if (method != null)
                    {
                        await (Task)method.Invoke(handler, new object[] { @event, ct });
                        
                        var handlerDuration = DateTime.UtcNow - handlerStartTime;
                        _logger.LogDebug(
                            "Handler {HandlerType} processed event {EventType} in {Duration}ms",
                            handler.GetType().Name,
                            @event.GetType().Name,
                            handlerDuration.TotalMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Handler {HandlerType} failed to process event {EventType} (Id: {EventId}, Depth: {Depth}, Chain: {Chain})",
                        handler.GetType().Name,
                        @event.GetType().Name,
                        @event.Id,
                        @event.Depth,
                        @event.EventChain);
                    throw;
                }
            }

            var totalDuration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Event {EventType} (Id: {EventId}, Depth: {Depth}) processed successfully in {Duration}ms",
                @event.GetType().Name,
                @event.Id,
                @event.Depth,
                totalDuration.TotalMilliseconds);
        }

        /// <summary>
        /// Retries event dispatch using exponentially increasing delays until it succeeds,
        /// cancellation is requested, or the configured attempt count is exhausted.
        /// </summary>
        /// <param name="@event">The event whose dispatch should be retried.</param>
        /// <param name="ct">The cancellation token used during delays and handler execution.</param>
        /// <returns>A task that completes after a successful retry or after all attempts fail.</returns>
        private async Task RetryEventProcessingAsync(IIntegrationEvent @event, CancellationToken ct)
        {
            for (int attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Retrying event {EventType} (Id: {EventId}), attempt {Attempt}/{MaxAttempts}",
                        @event.GetType().Name,
                        @event.Id,
                        attempt,
                        _options.MaxRetryAttempts);

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, ct);

                    await ProcessEventAsync(@event, ct);
                    
                    _logger.LogInformation(
                        "Event {EventType} (Id: {EventId}) processed successfully after {Attempt} retry attempts",
                        @event.GetType().Name,
                        @event.Id,
                        attempt);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Retry attempt {Attempt} failed for event {EventType} (Id: {EventId})",
                        attempt,
                        @event.GetType().Name,
                        @event.Id);

                    if (attempt == _options.MaxRetryAttempts)
                    {
                        _logger.LogError(
                            "Event {EventType} (Id: {EventId}) failed after {MaxAttempts} retry attempts",
                            @event.GetType().Name,
                            @event.Id,
                            _options.MaxRetryAttempts);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Controls event-bus concurrency, duplicate/depth/cycle protection, request timeout,
    /// and retry behavior for <see cref="EventBusHostedService"/>.
    /// </summary>
    public class EventBusOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of events processed concurrently.
        /// </summary>
        public int MaxConcurrency { get; set; } = Math.Max(4, Environment.ProcessorCount * 2);

        /// <summary>
        /// Gets or sets whether already-processed event identifiers are skipped.
        /// </summary>
        public bool EnableDuplicateDetection { get; set; } = true;

        /// <summary>
        /// Gets or sets whether handler failures should trigger retries.
        /// </summary>
        public bool RetryOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts after the initial dispatch.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets the maximum permitted event-chain depth.
        /// </summary>
        public int MaxEventChainDepth { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets whether an event type already present in its chain is rejected.
        /// </summary>
        public bool EnableCircularReferenceDetection { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum duration allowed for request/response event correlation.
        /// </summary>
        public TimeSpan MaxRequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
