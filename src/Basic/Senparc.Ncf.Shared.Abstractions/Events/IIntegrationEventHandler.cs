using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 事件处理器泛型接口
    /// </summary>
    /// <typeparam name="TIntegrationEvent">具体的事件类型</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent>
        where TIntegrationEvent : IIntegrationEvent
    {
        Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
    }
}