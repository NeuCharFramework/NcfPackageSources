using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 事件总线发布接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 异步发布事件（非阻塞，写入 Channel 即返回）
        /// </summary>
        ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}