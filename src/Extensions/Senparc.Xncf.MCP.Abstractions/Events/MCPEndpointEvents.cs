/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: MCPEndpointEvents.cs
    File description: Related implementation.


    Created by: Senparc - 20260327
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260924
    Change description: v0.1.0 Updated module behavior and related support.
    Legacy comment translated to English.

    Modified by: Senparc - 20261005
    Change description: v0.1.0 0.1.0 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.1.1 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.MCP.Abstractions.Events
{

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    ///Legacy comment translated to English.
    public sealed record QueryMcpEndpointsRequest(bool OnlyEnabled = true)
        : IntegrationRequest<QueryMcpEndpointsResponse>;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record QueryMcpEndpointsResponse(
        Guid RequestId,
        bool Success,
        string? Message,
        McpEndpointInfo[] Endpoints)
        : IntegrationResponse(RequestId);

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public sealed record McpEndpointsUpdatedEvent(
        string UpdateReason,
        int EndpointCount)
        : IntegrationEvent;
}
