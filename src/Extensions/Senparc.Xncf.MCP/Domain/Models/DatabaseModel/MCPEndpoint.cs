using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.MCP.Models.DatabaseModel
{
    /// <summary>
    /// MCP Endpoint 配置
    /// 存储可用的 MCP 端点信息，供 Agent 使用
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MCPEndpoint))]
    [Serializable]
    public class MCPEndpoint : EntityBase<int>
    {
        /// <summary>
        /// 端点名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 端点 URI/路径
        /// 例如: "http://localhost:3001/mcp/endpoint"
        /// 或 "sse://..." 或其他 MCP 协议支持的格式
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; }

        /// <summary>
        /// 端点类型
        /// 例如: "http", "sse", "stdio", "websocket"
        /// </summary>
        [MaxLength(50)]
        public string EndpointType { get; set; }

        /// <summary>
        /// 协议版本
        /// 例如: "1.0", "2.0"
        /// </summary>
        [MaxLength(20)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 认证信息 JSON
        /// 用于存储认证相关的配置，如 API Key、Token 等
        /// </summary>
        [MaxLength(1000)]
        public string AuthConfig { get; set; }

        /// <summary>
        /// 额外参数 JSON
        /// 用于存储其他自定义参数
        /// </summary>
        [MaxLength(2000)]
        public string ExtraConfig { get; set; }

        /// <summary>
        /// 最后测试时间
        /// </summary>
        public DateTime? LastTestedTime { get; set; }

        /// <summary>
        /// 最后测试结果
        /// true: 成功, false: 失败
        /// </summary>
        public bool? LastTestResult { get; set; }
    }
}
