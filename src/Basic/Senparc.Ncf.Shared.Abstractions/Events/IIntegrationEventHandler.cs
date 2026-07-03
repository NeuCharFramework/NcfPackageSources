/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IIntegrationEventHandler.cs
    文件功能描述：IIntegrationEventHandler 相关实现
    
    
    创建标识：Senparc - 20260216
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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