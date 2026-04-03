using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Integrated event marking interface
    /// </summary>
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime CreationDate { get; }
        
        /// <summary>
        /// Parent event ID (used for tracing event chains and detecting circular references)
        /// </summary>
        Guid? ParentEventId { get; }
        
        /// <summary>
        /// Event chain depth (root event is 0, each fork +1)
        /// </summary>
        int Depth { get; }
        
        /// <summary>
        /// Event type chain path (used to detect loops, format: EventType1→EventType2→...)
        /// </summary>
        string EventChain { get; }
    }

    /// <summary>
    ///Basic event class (it is recommended that all specific events inherit this class)
    /// </summary>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Parent event ID (used to track event chains)
        /// </summary>
        public Guid? ParentEventId { get; init; }
        
        /// <summary>
        /// Event chain depth (root event is 0)
        /// </summary>
        public int Depth { get; init; }
        
        /// <summary>
        /// Event type chain path (format: EventType1→EventType2→...)
        /// </summary>
        public string EventChain { get; init; } = string.Empty;

        /// <summary>
        /// Event summary information for debugging and logging
        /// </summary>
        public virtual string GetEventSummary()
        {
            return $"{GetType().Name}[{Id:N}] Depth={Depth}";
        }
        
        /// <summary>
        /// Derive new events from current events (inherit event chain information)
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
        /// Check if there is a loop in the event chain (the same event type appears twice)
        /// </summary>
        public bool HasCircularReference(string newEventType)
        {
            if (string.IsNullOrEmpty(EventChain))
            {
                return false;
            }
            
            var eventTypes = EventChain.Split('→').ToList();
            eventTypes.Add(newEventType);
            
            // Check if there are duplicate event types
            var duplicates = eventTypes.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
            return duplicates.Any();
        }
    }
    
    /// <summary>
    ///Event metadata (used to create derived events)
    /// </summary>
    public record EventMetadata(
        Guid ParentEventId,
        int Depth,
        string EventChain
    );
}