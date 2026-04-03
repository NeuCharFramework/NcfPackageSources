using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.MCP.Models.DatabaseModel
{
    /// <summary>
    ///MCP Endpoint Configuration
    /// Stores available MCP endpoint information for use by Agent
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MCPEndpoint))]
    [Serializable]
    public class MCPEndpoint : EntityBase<int>
    {
        /// <summary>
        /// endpoint name
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        ///endpoint_uri/path
        /// For example: "http://localhost:3001/mcp/endpoint"
        /// or "sse://..." or other format supported by MCP protocol
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        /// <summary>
        /// endpoint type
        /// Example: "http", "sse", "stdio", "websocket"
        /// </summary>
        [MaxLength(50)]
        public string EndpointType { get; set; }

        /// <summary>
        /// protocol version
        ///Example: "1.0", "2.0"
        /// </summary>
        [MaxLength(20)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        ///description information
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Whether to enable
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///Authentication information JSON
        /// Used to store authentication-related configurations, such as API Key, Token, etc.
        /// </summary>
        [MaxLength(1000)]
        public string AuthConfig { get; set; }

        /// <summary>
        ///Extra parameters JSON
        /// is used to store other custom parameters
        /// </summary>
        [MaxLength(2000)]
        public string ExtraConfig { get; set; }

        /// <summary>
        ///last test time
        /// </summary>
        public DateTime? LastTestedTime { get; set; }

        /// <summary>
        ///Last test results
        /// true: success, false: failure
        /// </summary>
        public bool? LastTestResult { get; set; }
    }
}
