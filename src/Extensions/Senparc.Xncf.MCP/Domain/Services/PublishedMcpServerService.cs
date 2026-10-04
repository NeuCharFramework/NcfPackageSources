/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PublishedMcpServerService.cs
    文件功能描述：读取本站运行时实际发布的 MCP 服务及访问端点

    创建标识：Senparc - 20261004
----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Senparc.Ncf.XncfBase.MCP;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.MCP.Domain.Services
{
    public class PublishedMcpServerDto
    {
        public string ServerName { get; set; } = string.Empty;
        public string XncfName { get; set; } = string.Empty;
        public string XncfUid { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public List<PublishedMcpEndpointDto> Endpoints { get; set; } = new List<PublishedMcpEndpointDto>();
    }

    public class PublishedMcpEndpointDto
    {
        public string EndpointType { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
    }

    public class PublishedMcpServerService
    {
        private readonly McpServerInfoCollection _registrations;
        private readonly EndpointDataSource _endpoints;

        public PublishedMcpServerService(McpServerInfoCollection registrations, EndpointDataSource endpoints)
        {
            _registrations = registrations;
            _endpoints = endpoints;
        }

        public List<PublishedMcpServerDto> GetPublishedServers(HttpRequest request)
        {
            var routes = _endpoints.Endpoints.OfType<RouteEndpoint>().ToArray();
            var result = new List<PublishedMcpServerDto>();
            foreach (var registration in _registrations.Values
                .Where(server => !string.IsNullOrWhiteSpace(server.McpRoute))
                .OrderBy(server => server.XncfName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(server => server.ServerName, StringComparer.OrdinalIgnoreCase))
            {
                var route = "/" + registration.McpRoute.Trim('/');
                var endpoints = new List<PublishedMcpEndpointDto>();
                AddEndpoint(route + "/sse", "GET", "sse");
                AddEndpoint(route, "POST", "http");
                if (endpoints.Count == 0)
                {
                    continue;
                }

                result.Add(new PublishedMcpServerDto
                {
                    ServerName = registration.ServerName,
                    XncfName = registration.XncfName,
                    XncfUid = registration.XncfUid,
                    Route = request.PathBase.Add(new PathString(route)).ToString(),
                    Endpoints = endpoints
                });

                void AddEndpoint(string path, string method, string type)
                {
                    var isMapped = routes.Any(endpoint =>
                    {
                        if (!string.Equals("/" + endpoint.RoutePattern.RawText?.Trim('/'), path, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                        return methods == null || methods.Count == 0
                            || methods.Contains(method, StringComparer.OrdinalIgnoreCase);
                    });
                    if (!isMapped)
                    {
                        return;
                    }

                    endpoints.Add(new PublishedMcpEndpointDto
                    {
                        EndpointType = type,
                        Endpoint = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, new PathString(path))
                    });
                }
            }
            return result;
        }
    }
}
