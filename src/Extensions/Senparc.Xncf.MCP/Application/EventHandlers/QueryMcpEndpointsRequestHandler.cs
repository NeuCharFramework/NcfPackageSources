/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：QueryMcpEndpointsRequestHandler.cs
    文件功能描述：处理来自其他模块（如 AgentsManager）的 MCP Endpoint 列表查询请求。
    通过 EventBus 请求-响应机制，将 MCP 模块登记的端点信息以只读 DTO 形式返回。


    创建标识：Senparc - 20260924

    修改标识：Senparc - 20260924
    修改描述：v0.5.6 新增 MCP Endpoint 跨模块查询处理器（EventBus 请求-响应）

----------------------------------------------------------------*/

using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.MCP.Abstractions;
using Senparc.Xncf.MCP.Abstractions.Events;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.Application.EventHandlers
{
    /// <summary>
    /// 处理 <see cref="QueryMcpEndpointsRequest"/>，将 MCP Endpoint 列表
    /// 以跨模块共享的 <see cref="McpEndpointInfo"/> 形式通过 EventBus 返回。
    /// </summary>
    public sealed class QueryMcpEndpointsRequestHandler
        : IIntegrationEventHandler<QueryMcpEndpointsRequest>
    {
        private readonly MCPEndpointService _mcpEndpointService;
        private readonly IEventBus _eventBus;

        public QueryMcpEndpointsRequestHandler(
            MCPEndpointService mcpEndpointService,
            IEventBus eventBus)
        {
            _mcpEndpointService = mcpEndpointService;
            _eventBus = eventBus;
        }

        public async Task Handle(QueryMcpEndpointsRequest @event, CancellationToken cancellationToken)
        {
            QueryMcpEndpointsResponse response;

            try
            {
                var endpoints = await _mcpEndpointService.GetFullListAsync(
                    x => true, "Name asc");

                var list = (endpoints?.ToList() ?? new List<MCPEndpoint>())
                    .Select(MapToInfo)
                    .Where(z => z != null)
                    .ToList();

                if (@event.OnlyEnabled)
                {
                    list = list.Where(z => z.Enabled).ToList();
                }

                response = new QueryMcpEndpointsResponse(
                    @event.RequestId,
                    true,
                    null,
                    list.ToArray());
            }
            catch (Exception ex)
            {
                response = new QueryMcpEndpointsResponse(
                    @event.RequestId,
                    false,
                    ex.Message,
                    Array.Empty<McpEndpointInfo>());
            }

            await _eventBus.PublishDerivedAsync(response, @event, cancellationToken);
        }

        private static McpEndpointInfo MapToInfo(MCPEndpoint entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new McpEndpointInfo(
                entity.Id,
                entity.Name,
                entity.Endpoint,
                entity.EndpointType,
                entity.ProtocolVersion,
                entity.Description,
                entity.Enabled,
                entity.LastTestedTime,
                entity.LastTestResult,
                entity.LastToolCount);
        }
    }
}
