/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IEventBus.cs
    文件功能描述：IEventBus 相关实现
    
    
    创建标识：Senparc - 20260216
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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