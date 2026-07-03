/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IIntegrationEvent.cs
    文件功能描述：IIntegrationEvent 相关实现
    
    
    创建标识：Senparc - 20260216
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 集成事件标记接口
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime CreationDate { get; }
        
        /// <summary>
        /// 父事件 ID（用于追踪事件链和检测循环引用）
        /// </summary>
        Guid? ParentEventId { get; }
        
        /// <summary>
        /// 事件链深度（根事件为 0，每次派生 +1）
        /// </summary>
        int Depth { get; }
        
        /// <summary>
        /// 事件类型链路径（用于检测循环，格式：EventType1→EventType2→...）
        /// </summary>
        string EventChain { get; }
    }

    /// <summary>
    /// 基础事件类（建议所有具体事件继承此类）
    /// </summary>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// 父事件 ID（用于追踪事件链）
        /// </summary>
        public Guid? ParentEventId { get; init; }
        
        /// <summary>
        /// 事件链深度（根事件为 0）
        /// </summary>
        public int Depth { get; init; }
        
        /// <summary>
        /// 事件类型链路径（格式：EventType1→EventType2→...）
        /// </summary>
        public string EventChain { get; init; } = string.Empty;

        /// <summary>
        /// 用于调试和日志记录的事件摘要信息
        /// </summary>
        public virtual string GetEventSummary()
        {
            return $"{GetType().Name}[{Id:N}] Depth={Depth}";
        }
        
        /// <summary>
        /// 从当前事件派生新事件（继承事件链信息）
        /// </summary>
        public EventMetadata DeriveMetadata()
        {
            var currentTypeName = GetType().Name;
            var newChain = string.IsNullOrEmpty(EventChain) 
                ? currentTypeName 
                : $"{EventChain}→{currentTypeName}";
            
            return new EventMetadata(Id, Depth + 1, newChain);
        }
        
        /// <summary>
        /// 检查事件链中是否存在循环（同一事件类型出现两次）
        /// </summary>
        public bool HasCircularReference(string newEventType)
        {
            if (string.IsNullOrEmpty(EventChain))
            {
                return false;
            }
            
            var eventTypes = EventChain.Split('→').ToList();
            eventTypes.Add(newEventType);
            
            // 检查是否有重复的事件类型
            var duplicates = eventTypes.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
            return duplicates.Any();
        }
    }
    
    /// <summary>
    /// 事件元数据（用于创建派生事件）
    /// </summary>
    public record EventMetadata(
        Guid ParentEventId,
        int Depth,
        string EventChain
    );
}