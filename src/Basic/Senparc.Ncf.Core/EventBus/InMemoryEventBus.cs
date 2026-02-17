using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Senparc.Ncf.Shared.Abstractions.Events; 

namespace Senparc.Ncf.Core.EventBus
{
    /// <summary>
    /// 基于内存 Channel 的事件总线实现
    /// </summary>
    public class InMemoryEventBus : IEventBus
    {
        private readonly Channel<IIntegrationEvent> _channel;

        public InMemoryEventBus()
        {
            // 配置无界通道（生产速度 > 消费速度时，内存会增加，但不会阻塞生产者）
            // 如果需要背压控制，可以使用 Channel.CreateBounded
            var options = new UnboundedChannelOptions
            {
                SingleReader = true,  // 只有一个后台服务在读
                SingleWriter = false  // 多个业务模块在并发写
            };
            _channel = Channel.CreateUnbounded<IIntegrationEvent>(options);
        }

        public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IIntegrationEvent
        {
            return _channel.Writer.WriteAsync(@event, cancellationToken);
        }

        // 供同一个程序集内的 HostedService 读取
        internal ChannelReader<IIntegrationEvent> Reader => _channel.Reader;
    }
}