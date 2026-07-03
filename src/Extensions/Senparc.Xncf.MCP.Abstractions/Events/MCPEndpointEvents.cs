/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MCPEndpointEvents.cs
    文件功能描述：MCPEndpointEvents 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.MCP.Abstractions.Events
{
    /// <summary>
    /// MCP Endpoint 查询请求事件
    /// AgentsManager 发送此事件来请求 MCP 模块返回可用的 Endpoints
    /// </summary>
    public class QueryMCPEndpointsEvent : IIntegrationEvent
    {
        /// <summary>
        /// 事件 ID（用于追踪响应）
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 是否仅返回已启用的端点
        /// </summary>
        public bool OnlyEnabled { get; set; } = true;

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }

    /// <summary>
    /// MCP Endpoint 更新事件
    /// MCP 模块发送此事件通知端点的更新
    /// </summary>
    public class MCPEndpointsUpdatedEvent : IIntegrationEvent
    {
        /// <summary>
        /// 端点列表 JSON（包含所有有效端点的信息）
        /// 格式：
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
        /// 更新的端点数量
        /// </summary>
        public int EndpointCount { get; set; }

        /// <summary>
        /// 触发更新的原因
        /// 例如: "Created", "Updated", "Deleted", "Enabled", "Disabled"
        /// </summary>
        public string UpdateReason { get; set; }

        public DateTime CreatedOn => DateTime.UtcNow;

        public string CorrelationId { get; set; }

        public string CausationId { get; set; }
    }

    /// <summary>
    /// MCP Endpoint 创建事件
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
    /// MCP Endpoint 启用/禁用事件
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
