/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: QueryMcpEndpointsRequestHandler.cs
    File description: Related implementation.
    Legacy comment translated to English.


    Created by: Senparc - 20260924

    Modified by: Senparc - 20260924
    Change description: v0.5.6 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.5.6 0.5.6 feat: add MCP endpoint selection feature in AgentsManager

    Modified by: Senparc - 20261103
    Change description: v0.5.10 Updated module behavior and related support.

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
