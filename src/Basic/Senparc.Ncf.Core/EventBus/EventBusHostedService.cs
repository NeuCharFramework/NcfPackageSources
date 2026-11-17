/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: EventBusHostedService.cs
    File description: EventBusHostedService related implementation
    
    
    Created by: Senparc - 20260215
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260726
    Change description: v0.25.0-preview2 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class EventBusHostedService : BackgroundService
    {
        private readonly InMemoryEventBus _eventBus;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBusHostedService> _logger;
        private readonly EventBusOptions _options;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Senparc NCF EventBus Service is starting with MaxConcurrency={MaxConcurrency}, EnableDuplicateDetection={EnableDuplicateDetection}, MaxEventChainDepth={MaxEventChainDepth}, EnableCircularReferenceDetection={EnableCircularReferenceDetection}", 
                _options.MaxConcurrency, 
                _options.EnableDuplicateDetection,
                _options.MaxEventChainDepth,
                _options.EnableCircularReferenceDetection);

            //Legacy comment translated to English.
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var activeTasks = new List<Task>();

            try
            {
                await foreach (var @event in _eventBus.Reader.ReadAllAsync(stoppingToken))
                {
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
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

                    //Legacy comment translated to English.
                    await semaphore.WaitAsync(stoppingToken);

                    //Legacy comment translated to English.
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
                            
                            //Legacy comment translated to English.
                            if (_options.RetryOnFailure && _options.MaxRetryAttempts > 0)
                            {
                                await RetryEventProcessingAsync(@event, stoppingToken);
                            }
                        }
                        finally
                        {
                            //Legacy comment translated to English.
                            semaphore.Release();
                        }
                    }, stoppingToken);

                    activeTasks.Add(task);

                    //Legacy comment translated to English.
                    if (activeTasks.Count >= _options.MaxConcurrency * 2)
                    {
                        activeTasks.RemoveAll(t => t.IsCompleted);
                    }
                }

                //Legacy comment translated to English.
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

        private async Task ProcessEventAsync(IIntegrationEvent @event, CancellationToken ct)
        {
            var startTime = DateTime.UtcNow;

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var completedPendingRequest = _eventBus.TryCompleteRequest(@event);
            
            //Legacy comment translated to English.
            using var scope = _serviceProvider.CreateScope();

            //Legacy comment translated to English.
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
                    throw; //Legacy comment translated to English.
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

                    //Legacy comment translated to English.
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, ct);

                    await ProcessEventAsync(@event, ct);
                    
                    _logger.LogInformation(
                        "Event {EventType} (Id: {EventId}) processed successfully after {Attempt} retry attempts",
                        @event.GetType().Name,
                        @event.Id,
                        attempt);
                    return; //Legacy comment translated to English.
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
    ///Legacy comment translated to English.
    /// </summary>
    public class EventBusOptions
    {
        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxConcurrency { get; set; } = Math.Max(4, Environment.ProcessorCount * 2);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool EnableDuplicateDetection { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool RetryOnFailure { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxEventChainDepth { get; set; } = 10;
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool EnableCircularReferenceDetection { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public TimeSpan MaxRequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
