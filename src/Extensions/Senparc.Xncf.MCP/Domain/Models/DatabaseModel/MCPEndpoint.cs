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

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.MCP.Models.DatabaseModel
{
    /// <summary>
    /// MCP Endpoint configuration
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MCPEndpoint))]
    [Serializable]
    public class MCPEndpoint : EntityBase<int>
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// for example: "http://localhost:3001/mcp/endpoint"
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// for example: "http", "sse", "stdio", "websocket"
        /// </summary>
        [MaxLength(50)]
        public string EndpointType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// for example: "1.0", "2.0"
        /// </summary>
        [MaxLength(20)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// descriptioninformation
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// whether enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(1000)]
        public string AuthConfig { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(2000)]
        public string ExtraConfig { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? LastTestedTime { get; set; }

        /// <summary>
        /// finallytestresult
        /// true: success, false: failure
        /// </summary>
        public bool? LastTestResult { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string? LastToolsJson { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? LastToolCount { get; set; }
    }
}
