/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpEndpoint.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Creation description: Related implementation.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

namespace Senparc.Xncf.AgentsManager.Domain.Models;

/// <summary>
/// Describes an MCP endpoint URL bound to an Agent configuration.
/// </summary>
public class McpEndpoint
{
    /// <summary>Gets or sets the MCP endpoint URL.</summary>
    public string url { get; set; }
}
