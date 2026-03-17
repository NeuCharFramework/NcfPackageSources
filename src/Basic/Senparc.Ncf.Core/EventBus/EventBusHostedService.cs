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
    /// 后台消息泵：负责从 Channel 读取事件并分发给 Handler
    /// 支持高并发场景，可配置并发度
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
                "Senparc NCF EventBus Service is starting with MaxConcurrency={MaxConcurrency}, EnableDuplicateDetection={EnableDuplicateDetection}", 
                _options.MaxConcurrency, 
                _options.EnableDuplicateDetection);

            // 使用信号量控制并发度，防止过多任务同时执行导致资源耗尽
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var activeTasks = new List<Task>();

            try
            {
                await foreach (var @event in _eventBus.Reader.ReadAllAsync(stoppingToken))
                {
                    // 防止重复处理检测
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

                    // 等待获取信号量（如果当前并发数已达上限）
                    await semaphore.WaitAsync(stoppingToken);

                    // 启动异步任务处理事件
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
                                "Error processing event {EventType} (Id: {EventId})", 
                                @event.GetType().Name, 
                                @event.Id);
                            
                            // 根据配置决定是否重试
                            if (_options.RetryOnFailure && _options.MaxRetryAttempts > 0)
                            {
                                await RetryEventProcessingAsync(@event, stoppingToken);
                            }
                        }
                        finally
                        {
                            // 释放信号量，允许下一个事件处理
                            semaphore.Release();
                        }
                    }, stoppingToken);

                    activeTasks.Add(task);

                    // 定期清理已完成的任务，防止内存泄漏
                    if (activeTasks.Count >= _options.MaxConcurrency * 2)
                    {
                        activeTasks.RemoveAll(t => t.IsCompleted);
                    }
                }

                // 等待所有正在处理的任务完成
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
            
            // 创建 Scope 以支持 Scoped 服务注入（如 DbContext, Repository）
            using var scope = _serviceProvider.CreateScope();

            // 动态查找：IIntegrationEventHandler<MyEvent>
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType).ToList();

            if (!handlers.Any())
            {
                _logger.LogWarning(
                    "No handler found for event {EventType} (Id: {EventId})", 
                    @event.GetType().Name, 
                    @event.Id);
                return;
            }

            _logger.LogDebug(
                "Processing event {EventType} (Id: {EventId}) with {HandlerCount} handler(s)",
                @event.GetType().Name,
                @event.Id,
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
                        "Handler {HandlerType} failed to process event {EventType} (Id: {EventId})",
                        handler.GetType().Name,
                        @event.GetType().Name,
                        @event.Id);
                    throw; // 重新抛出异常，触发重试机制
                }
            }

            var totalDuration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Event {EventType} (Id: {EventId}) processed successfully in {Duration}ms",
                @event.GetType().Name,
                @event.Id,
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

                    // 指数退避策略
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, ct);

                    await ProcessEventAsync(@event, ct);
                    
                    _logger.LogInformation(
                        "Event {EventType} (Id: {EventId}) processed successfully after {Attempt} retry attempts",
                        @event.GetType().Name,
                        @event.Id,
                        attempt);
                    return; // 成功处理，退出重试循环
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
    /// EventBus 配置选项
    /// </summary>
    public class EventBusOptions
    {
        /// <summary>
        /// 最大并发处理数（默认值根据 CPU 核心数计算）
        /// 建议：数据库连接池大小 / 2，或者 Environment.ProcessorCount * 2
        /// </summary>
        public int MaxConcurrency { get; set; } = Math.Max(4, Environment.ProcessorCount * 2);

        /// <summary>
        /// 是否启用重复事件检测（防止同一事件被处理多次）
        /// </summary>
        public bool EnableDuplicateDetection { get; set; } = true;

        /// <summary>
        /// 处理失败时是否重试
        /// </summary>
        public bool RetryOnFailure { get; set; } = true;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
    }
}