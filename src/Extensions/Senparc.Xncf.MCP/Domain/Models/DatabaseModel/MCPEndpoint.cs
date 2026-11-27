/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MCPEndpoint.cs
    File description: MCPEndpoint related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261005
    Change description: v0.5.6 0.5.6 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.5.10 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.5.14.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.MCP.Models.DatabaseModel
{
    /// <summary>
    /// Persists one configured MCP endpoint and its last discovery result.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MCPEndpoint))]
    [Serializable]
    public class MCPEndpoint : EntityBase<int>
    {
        [Required]
        [MaxLength(100)]
        /// <summary>
        /// Gets or sets the administrator-facing endpoint name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the MCP endpoint address, such as an HTTP or local transport URL.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the transport type, such as <c>http</c>, <c>sse</c>, <c>stdio</c>, or <c>websocket</c>.
        /// </summary>
        [MaxLength(50)]
        public string EndpointType { get; set; }

        /// <summary>
        /// Gets or sets the MCP protocol version expected by the endpoint.
        /// </summary>
        [MaxLength(20)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// Gets or sets the administrator-facing endpoint description.
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this endpoint is available for selection.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the serialized authentication configuration used by the transport.
        /// Secrets must be protected or otherwise handled according to the endpoint service contract.
        /// </summary>
        [MaxLength(1000)]
        public string AuthConfig { get; set; }

        /// <summary>
        /// Gets or sets serialized transport-specific configuration.
        /// </summary>
        [MaxLength(2000)]
        public string ExtraConfig { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the most recent endpoint test.
        /// </summary>
        public DateTime? LastTestedTime { get; set; }

        /// <summary>
        /// Gets or sets whether the most recent endpoint test succeeded.
        /// </summary>
        public bool? LastTestResult { get; set; }

        /// <summary>
        /// Gets or sets the serialized tool catalog returned by the most recent test.
        /// </summary>
        public string? LastToolsJson { get; set; }

        /// <summary>
        /// Gets or sets the number of tools returned by the most recent test.
        /// </summary>
        public int? LastToolCount { get; set; }
    }
}
