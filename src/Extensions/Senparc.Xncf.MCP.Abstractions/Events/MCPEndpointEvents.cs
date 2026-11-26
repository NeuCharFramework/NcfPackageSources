/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MCPEndpointEvents.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260924
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.1.0 0.1.0 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.1.1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.MCP.Abstractions.Events
{

    /// <summary>
    /// Requests the current MCP endpoint catalog through EventBus.
    /// </summary>
    /// <param name="OnlyEnabled">Whether disabled endpoints should be excluded.</param>
    public sealed record QueryMcpEndpointsRequest(bool OnlyEnabled = true)
        : IntegrationRequest<QueryMcpEndpointsResponse>;

    /// <summary>
    /// Returns the MCP endpoint catalog for a query request.
    /// </summary>
    /// <param name="RequestId">The request correlation identifier.</param>
    /// <param name="Success">Whether endpoint lookup succeeded.</param>
    /// <param name="Message">An optional failure or diagnostic message.</param>
    /// <param name="Endpoints">The endpoint information returned by the query.</param>
    public sealed record QueryMcpEndpointsResponse(
        Guid RequestId,
        bool Success,
        string? Message,
        McpEndpointInfo[] Endpoints)
        : IntegrationResponse(RequestId);

    /// <summary>
    /// Announces that configured MCP endpoint data changed.
    /// </summary>
    /// <param name="UpdateReason">The reason for the update.</param>
    /// <param name="EndpointCount">The number of configured endpoints after the update.</param>
    public sealed record McpEndpointsUpdatedEvent(
        string UpdateReason,
        int EndpointCount)
        : IntegrationEvent;
}
