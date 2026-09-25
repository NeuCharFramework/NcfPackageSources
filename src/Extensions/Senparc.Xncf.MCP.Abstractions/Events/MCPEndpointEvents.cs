/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MCPEndpointEvents.cs
    文件功能描述：MCP Endpoint 跨模块 EventBus 契约（请求-响应 + 变更通知）


    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260924
    修改描述：v0.1.0 迁移到 IntegrationRequest/IntegrationResponse 标准契约，
    支持 AgentsManager 通过 IEventBusRequestClient 查询 MCP Endpoint 列表

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.MCP.Abstractions.Events
{

    /// <summary>
    /// 请求通过 EventBus 获取 MCP 模块中登记的 Endpoint 列表。
    /// AgentsManager 等模块在“从列表选择 MCP”时使用该请求。
    /// </summary>
    /// <param name="OnlyEnabled">为 true 时仅返回已启用的 Endpoint。</param>
    public sealed record QueryMcpEndpointsRequest(bool OnlyEnabled = true)
        : IntegrationRequest<QueryMcpEndpointsResponse>;

    /// <summary>
    /// <see cref="QueryMcpEndpointsRequest"/> 的响应。
    /// </summary>
    public sealed record QueryMcpEndpointsResponse(
        Guid RequestId,
        bool Success,
        string? Message,
        McpEndpointInfo[] Endpoints)
        : IntegrationResponse(RequestId);

    /// <summary>
    /// MCP Endpoint 发生增删改或启用状态变化时的通知事件。
    /// 订阅模块可以借此刷新缓存的列表。
    /// </summary>
    public sealed record McpEndpointsUpdatedEvent(
        string UpdateReason,
        int EndpointCount)
        : IntegrationEvent;
}
