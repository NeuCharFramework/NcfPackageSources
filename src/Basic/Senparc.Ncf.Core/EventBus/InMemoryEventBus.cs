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
    public class InMemoryEventBus : IEventBus
    {
        private readonly Channel<IIntegrationEvent> _channel;
        private readonly ILogger<InMemoryEventBus> _logger;
        
        // 用于防止重复处理的事件 ID 追踪（使用滑动窗口，保留最近 10 分钟的事件 ID）
        private readonly ConcurrentDictionary<Guid, DateTime> _processedEventIds = new();
        private readonly TimeSpan _eventIdRetentionPeriod = TimeSpan.FromMinutes(10);
        
        public InMemoryEventBus(ILogger<InMemoryEventBus> logger = null)
        {
            _logger = logger;
            
            // 配置无界通道（生产速度 > 消费速度时，内存会增加，但不会阻塞生产者）
            // 如果需要背压控制，可以使用 Channel.CreateBounded
            var options = new UnboundedChannelOptions
            {
                SingleReader = false,  // 支持多个消费者并发读取
                SingleWriter = false   // 多个业务模块在并发写
            };
            _channel = Channel.CreateUnbounded<IIntegrationEvent>(options);
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
    }
}