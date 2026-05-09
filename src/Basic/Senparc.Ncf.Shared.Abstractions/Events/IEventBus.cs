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
            
        /// <summary>
        /// 异步发布派生事件（自动继承父事件的链信息，用于防止循环引用）
        /// </summary>
        ValueTask PublishDerivedAsync<TEvent>(TEvent @event, IIntegrationEvent parentEvent, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent;
    }
}