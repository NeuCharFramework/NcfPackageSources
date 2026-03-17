using System;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 集成事件标记接口
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime CreationDate { get; }
    }

    /// <summary>
    /// 基础事件类（建议所有具体事件继承此类）
    /// </summary>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreationDate { get; } = DateTime.UtcNow;

        /// <summary>
        /// 用于调试和日志记录的事件摘要信息
        /// </summary>
        public virtual string GetEventSummary()
        {
            return $"{GetType().Name}[{Id:N}]";
        }
    }
}