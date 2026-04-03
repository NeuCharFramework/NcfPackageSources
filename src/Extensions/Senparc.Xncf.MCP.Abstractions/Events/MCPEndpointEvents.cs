using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.MCP.Abstractions.Events
{
    /// <summary>
    /// MCP Endpoint Query request event
    /// AgentsManager Send this event to request the MCP module to return available Endpoints
    /// </summary>
    public class QueryMCPEndpointsEvent : IIntegrationEvent
    {
        /// <summary>
        /// Event ID (used to track responses)
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Whether to return only enabled endpoints
        /// </summary>
        public bool OnlyEnabled { get; set; } = true;

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }

    /// <summary>
    /// MCP Endpoint Update event
    /// MCP The module sends this event to notify the endpoint of updates
    /// </summary>
    public class MCPEndpointsUpdatedEvent : IIntegrationEvent
    {
        /// <summary>
        /// Endpoint list JSON (contains information about all valid endpoints)
        /// Format:
        /// [
        ///   {
        ///     "id": 1,
        ///     "name": "endpoint1",
        ///     "endpoint": "http://...",
        ///     "enabled": true,
        ///     ...
        ///   }
        /// ]
        /// </summary>
        public string EndpointsJson { get; set; }

        /// <summary>
        /// Number of updated endpoints
        /// </summary>
        public int EndpointCount { get; set; }

        /// <summary>
        /// What triggered the update
        /// For example: "Created", "Updated", "Deleted", "Enabled", "Disabled"
        /// </summary>
        public string UpdateReason { get; set; }

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }

    /// <summary>
    /// MCP Endpoint Create event
    /// </summary>
    public class MCPEndpointCreatedEvent : IIntegrationEvent
    {
        public int EndpointId { get; set; }
        public string Name { get; set; }
        public string Endpoint { get; set; }

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }

    /// <summary>
    /// MCP Endpoint enable/Disable event
    /// </summary>
    public class MCPEndpointStatusChangedEvent : IIntegrationEvent
    {
        public int EndpointId { get; set; }
        public string Name { get; set; }
        public bool IsEnabled { get; set; }

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }
}
