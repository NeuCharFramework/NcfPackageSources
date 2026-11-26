/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpEndpointInfo.cs
    File description: Related implementation.

    Created by: Senparc - 20260924

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

namespace Senparc.Xncf.MCP.Abstractions;

/// <summary>
/// Describes one configured MCP endpoint and the latest connectivity/tool-discovery status.
/// </summary>
/// <param name="Id">The persisted endpoint identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="Endpoint">The configured transport endpoint.</param>
/// <param name="EndpointType">The transport type, such as SSE or streamable HTTP.</param>
/// <param name="ProtocolVersion">The configured protocol version.</param>
/// <param name="Description">The administrator description.</param>
/// <param name="Enabled">Whether the endpoint is enabled for selection.</param>
/// <param name="LastTestedTime">The last connectivity-test time.</param>
/// <param name="LastTestResult">Whether the last connectivity test succeeded.</param>
/// <param name="LastToolCount">The number of tools discovered by the last successful test.</param>
public sealed record McpEndpointInfo(
    int Id,
    string Name,
    string Endpoint,
    string? EndpointType,
    string? ProtocolVersion,
    string? Description,
    bool Enabled,
    DateTime? LastTestedTime,
    bool? LastTestResult,
    int? LastToolCount);
