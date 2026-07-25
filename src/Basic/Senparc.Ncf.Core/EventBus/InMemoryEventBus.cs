/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：InMemoryEventBus.cs
    文件功能描述：InMemoryEventBus 相关实现
    
    
    创建标识：Senparc - 20260215
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// 基于内存 Channel 的事件总线实现
    /// </summary>
    public class InMemoryEventBus : IEventBus, IEventBusRequestClient
    {
        private readonly Channel<IIntegrationEvent> _channel;
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly TimeSpan _maxRequestTimeout;
        private readonly ConcurrentDictionary<Guid, PendingRequest> _pendingRequests = new();
        
        // 用于防止重复处理的事件 ID 追踪（使用滑动窗口，保留最近 10 分钟的事件 ID）
        private readonly ConcurrentDictionary<Guid, DateTime> _processedEventIds = new();
        private readonly TimeSpan _eventIdRetentionPeriod = TimeSpan.FromMinutes(10);
        
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
            
            // 配置无界通道（生产速度 > 消费速度时，内存会增加，但不会阻塞生产者）
            // 如果需要背压控制，可以使用 Channel.CreateBounded
            var channelOptions = new UnboundedChannelOptions
            {
                SingleReader = true,   // HostedService 是唯一消息泵；Handler 并发由消息泵负责
                SingleWriter = false   // 多个业务模块在并发写
            };
            _channel = Channel.CreateUnbounded<IIntegrationEvent>(channelOptions);
        }

        /// <summary>
        /// 发布请求并等待匹配类型及 RequestId 的响应。
        /// </summary>
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

        public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IIntegrationEvent
        {
            return _channel.Writer.WriteAsync(@event, cancellationToken);
        }
        
        /// <summary>
        /// 发布派生事件（自动继承父事件的链信息并检测循环）
        /// 注意：此方法要求事件类型继承自 IntegrationEvent 基类
        /// </summary>
        public ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            // 仅支持 IntegrationEvent 基类（因为需要访问 DeriveMetadata 等方法）
            if (parentEvent is not IntegrationEvent typedParent)
            {
                throw new ArgumentException("Parent event must inherit from IntegrationEvent base class", nameof(parentEvent));
            }
            
            if (@event is not IntegrationEvent typedEvent)
            {
                throw new ArgumentException("Event must inherit from IntegrationEvent base class", nameof(@event));
            }
            
            // 派生事件的元数据
            var metadata = typedParent.DeriveMetadata();
            
            // 检查是否会产生循环引用（在发布前预检）
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
            
            // 创建一个带有链信息的新事件实例
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
        /// 尝试用响应事件完成对应的等待请求。EventBusHostedService 是唯一调用方，
        /// 避免额外读取 Channel 而与主消息泵竞争。
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
        /// 检查事件是否已经被处理过（用于防止重复处理）
        /// </summary>
        public bool TryMarkEventAsProcessed(Guid eventId)
        {
            // 清理过期的事件 ID（每100次调用清理一次）
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

        // 供同一个程序集内的 HostedService 读取
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
