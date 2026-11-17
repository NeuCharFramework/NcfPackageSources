/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: IIntegrationEvent.cs
    File description: IIntegrationEvent related implementation
    
    
    Created by: Senparc - 20260216
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime CreationDate { get; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Guid? ParentEventId { get; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        int Depth { get; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string EventChain { get; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Guid? ParentEventId { get; init; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Depth { get; init; }
        
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string EventChain { get; init; } = string.Empty;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public virtual string GetEventSummary()
        {
            return $"{GetType().Name}[{Id:N}] Depth={Depth}";
        }
        
        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public bool HasCircularReference(string newEventType)
        {
            if (string.IsNullOrEmpty(EventChain))
            {
                return false;
            }
            
            var eventTypes = EventChain.Split('→').ToList();
            eventTypes.Add(newEventType);
            
            //Legacy comment translated to English.
            var duplicates = eventTypes.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
            return duplicates.Any();
        }
    }
    
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record EventMetadata(
        Guid ParentEventId,
        int Depth,
        string EventChain
    );
}