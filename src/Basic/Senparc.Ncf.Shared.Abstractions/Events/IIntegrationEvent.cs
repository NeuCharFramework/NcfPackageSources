/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIntegrationEvent.cs
    File description: IIntegrationEvent related implementation

    Created by: Senparc - 20260216

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// Defines the metadata required for an event dispatched through the NCF integration bus.
    /// </summary>
    public interface IIntegrationEvent
    {
        /// <summary>Gets the unique identifier of this event instance.</summary>
        Guid Id { get; }
        /// <summary>Gets the UTC creation timestamp of this event.</summary>
        DateTime CreationDate { get; }
        
        /// <summary>
        /// Gets the identifier of the event that caused this event, when it is derived.
        /// </summary>
        Guid? ParentEventId { get; }
        
        /// <summary>
        /// Gets the number of derivation levels from the original event.
        /// </summary>
        int Depth { get; }
        
        /// <summary>
        /// Gets the delimiter-separated event type chain used to detect circular dispatch.
        /// </summary>
        string EventChain { get; }
    }

    /// <summary>
    /// Provides default identity and derivation metadata for integration events.
    /// </summary>
    public abstract record IntegrationEvent : IIntegrationEvent
    {
        /// <summary>Gets or initializes the unique event identifier.</summary>
        public Guid Id { get; init; } = Guid.NewGuid();
        /// <summary>Gets or initializes the UTC event creation timestamp.</summary>
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or initializes the parent event identifier.
        /// </summary>
        public Guid? ParentEventId { get; init; }
        
        /// <summary>
        /// Gets or initializes the event derivation depth.
        /// </summary>
        public int Depth { get; init; }
        
        /// <summary>
        /// Gets or initializes the delimiter-separated event type chain.
        /// </summary>
        public string EventChain { get; init; } = string.Empty;

        /// <summary>
        /// Returns a concise diagnostic representation containing the event type, ID, and depth.
        /// </summary>
        public virtual string GetEventSummary()
        {
            return $"{GetType().Name}[{Id:N}] Depth={Depth}";
        }
        
        /// <summary>
        /// Creates metadata for an event derived from this event.
        /// </summary>
        /// <returns>The parent ID, incremented depth, and extended event chain.</returns>
        public EventMetadata DeriveMetadata()
        {
            var currentTypeName = GetType().Name;
            var newChain = string.IsNullOrEmpty(EventChain) 
                ? currentTypeName 
                : $"{EventChain}→{currentTypeName}";
            
            return new EventMetadata(Id, Depth + 1, newChain);
        }
        
        /// <summary>
        /// Determines whether adding an event type would repeat a type already in the event chain.
        /// </summary>
        /// <param name="newEventType">The event type that would be appended.</param>
        /// <returns><see langword="true"/> when the resulting chain contains a duplicate type.</returns>
        public bool HasCircularReference(string newEventType)
        {
            if (string.IsNullOrEmpty(EventChain))
            {
                return false;
            }
            
            var eventTypes = EventChain.Split('→').ToList();
            eventTypes.Add(newEventType);
            
            var duplicates = eventTypes.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
            return duplicates.Any();
        }
    }
    
    /// <summary>
    /// Carries the metadata needed to initialize a derived integration event.
    /// </summary>
    /// <param name="ParentEventId">The event ID that caused the derived event.</param>
    /// <param name="Depth">The derived event depth.</param>
    /// <param name="EventChain">The extended event type chain.</param>
    public record EventMetadata(
        Guid ParentEventId,
        int Depth,
        string EventChain
    );
}
