/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MCPEndpointAppService.cs
    File description: MCPEndpointAppService related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.4.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261003
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261004
    Change description: v0.5.8 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 Remove DatabaseSample and related components from MCP extension

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.MCP.Abstractions.Events;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.OHS.Local.AppService
{
    /// <summary>
    /// MCP Endpoint management AppService
    ///Legacy comment translated to English.
    /// </summary>
    [ApiAuthorize]
    public class MCPEndpointAppService : AppServiceBase
    {
        private readonly MCPEndpointService _mcpEndpointService;

        public MCPEndpointAppService(IServiceProvider serviceProvider, MCPEndpointService mcpEndpointService)
            : base(serviceProvider)
        {
            _mcpEndpointService = mcpEndpointService;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<PublishedMcpServerDto>>> GetPublishedServers()
        {
            return await this.GetResponseAsync<List<PublishedMcpServerDto>>((response, logger) =>
            {
                var context = GetRequiredService<IHttpContextAccessor>().HttpContext
                    ?? throw new NcfExceptionBase("无法获取当前请求，不能生成本站 MCP 服务地址");
                var servers = GetRequiredService<PublishedMcpServerService>().GetPublishedServers(context.Request);
                logger.Append(McpResource.Format("MCP.Server.CountPublished", "获取了 {0} 个本站发布的 MCP 服务", servers.Count));
                return Task.FromResult(servers);
            });
        }

        /// <summary>
        /// getall MCP Endpoints
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetAllEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetFullListAsync(x => true);
                var dtos = endpoints?.Select(e => MCPEndpointDto.FromEntity(e)).ToList() ?? new List<MCPEndpointDto>();
                
                logger.Append(McpResource.Format("MCP.Endpoint.CountAll", "获取了 {0} 个 MCP Endpoints", dtos.Count));
                return dtos;
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetEnabledEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetEnabledEndpointsAsync();
                var dtos = endpoints.Select(e => MCPEndpointDto.FromEntity(e)).ToList();
                
                logger.Append(McpResource.Format("MCP.Endpoint.CountEnabled", "获取了 {0} 个已启用的 MCP Endpoints", dtos.Count));
                return dtos;
            });
        }

        /// <summary>
        /// create/edit MCP Endpoint
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SaveEndpoint(MCPEndpointCreateOrEditRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                // validationinput
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.NameRequired"));
                }

                if (string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.AddressRequired"));
                }

                if (request.Id < 0)
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.InvalidId"));
                }

                request.Name = request.Name.Trim();
                request.Endpoint = request.Endpoint.Trim();
                Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

                var existing = await _mcpEndpointService.GetObjectAsync(
                    x => x.Name == request.Name && x.Id != request.Id);
                if (existing != null)
                {
                    throw new NcfExceptionBase(McpResource.Format("MCP.Endpoint.NameExists", "端点名称“{0}”已存在", request.Name));
                }

                MCPEndpoint endpoint;
                if (request.Id > 0)
                {
                    //Legacy comment translated to English.
                    endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                    if (endpoint == null)
                    {
                        throw new NcfExceptionBase(McpResource.Format("MCP.Endpoint.IdNotFound", "端点 ID {0} 不存在", request.Id));
                    }
                }
                else
                {
                    //Legacy comment translated to English.
                    endpoint = new MCPEndpoint();
                }

                if (endpoint.Endpoint != request.Endpoint
                    || endpoint.EndpointType != request.EndpointType
                    || endpoint.AuthConfig != request.AuthConfig
                    || endpoint.ExtraConfig != request.ExtraConfig)
                {
                    endpoint.LastTestedTime = null;
                    endpoint.LastTestResult = null;
                    endpoint.LastToolCount = null;
                    endpoint.LastToolsJson = null;
                }

                // updateproperty
                endpoint.Name = request.Name;
                endpoint.Endpoint = request.Endpoint;
                endpoint.EndpointType = request.EndpointType;
                endpoint.ProtocolVersion = request.ProtocolVersion;
                endpoint.Description = request.Description;
                endpoint.Enabled = request.Enabled;
                endpoint.AuthConfig = request.AuthConfig;
                endpoint.ExtraConfig = request.ExtraConfig;

                await _mcpEndpointService.SaveObjectAsync(endpoint);
                logger.Append(McpResource.Format(
                    request.Id > 0 ? "MCP.Endpoint.Updated" : "MCP.Endpoint.Created",
                    request.Id > 0 ? "✓ MCP Endpoint“{0}”已更新" : "✓ MCP Endpoint“{0}”已创建",
                    endpoint.Name));

                await PublishEndpointsUpdatedAsync(request.Id > 0 ? "EndpointUpdated" : "EndpointCreated");

                return logger.ToString();
            });
        }

        /// <summary>
        /// delete MCP Endpoint
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> DeleteEndpoint(MCPEndpointDeleteRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.InvalidId"));
                }

                var endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                if (endpoint == null)
                {
                    throw new NcfExceptionBase(McpResource.Format("MCP.Endpoint.IdNotFound", "端点 ID {0} 不存在", request.Id));
                }

                await _mcpEndpointService.DeleteObjectAsync(endpoint);
                logger.Append(McpResource.Format("MCP.Endpoint.Deleted", "✓ MCP Endpoint“{0}”已删除", endpoint.Name));

                await PublishEndpointsUpdatedAsync("EndpointDeleted");

                return logger.ToString();
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<McpConnectionTestResult>> TestEndpoint(MCPEndpointTestRequest request)
        {
            return await this.GetResponseAsync<McpConnectionTestResult>(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.InvalidId"));
                }

                var result = await _mcpEndpointService.TestEndpointAsync(request.Id, CancellationToken);

                logger.Append(result.Success
                    ? $"✓ {result.StatusMessage}"
                    : $"✗ {result.StatusMessage}");

                return result;
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private async Task PublishEndpointsUpdatedAsync(string updateReason)
        {
            var eventBus = GetService<IEventBus>();
            if (eventBus == null)
            {
                return;
            }

            var count = (await _mcpEndpointService.GetFullListAsync(x => true)).Count;
            await eventBus.PublishAsync(new McpEndpointsUpdatedEvent(updateReason, count));
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<McpConnectionTestResult>> TestConnection(MCPEndpointTestConnectionRequest request)
        {
            return await this.GetResponseAsync<McpConnectionTestResult>(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    throw new NcfExceptionBase(McpResource.Get("MCP.Endpoint.AddressRequired"));
                }

                Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
                var tester = base.GetRequiredService<McpConnectionTestService>();
                var result = await tester.TestAsync(
                    string.IsNullOrWhiteSpace(request.Name) ? "MCP-Test" : request.Name,
                    request.Endpoint.Trim(),
                    MCPEndpointService.ExtractBearerToken(request.AuthConfig),
                    endpointType: request.EndpointType,
                    cancellationToken: CancellationToken);

                logger.Append(result.Success
                    ? $"✓ {result.StatusMessage}"
                    : $"✗ {result.StatusMessage}");

                return result;
            });
        }
    }

    /// <summary>
    /// MCP Endpoint DTO
    /// </summary>
    public class MCPEndpointDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Endpoint { get; set; }
        public string EndpointType { get; set; }
        public string ProtocolVersion { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastTestedTime { get; set; }
        public bool? LastTestResult { get; set; }
        public int? LastToolCount { get; set; }
        public string? AuthConfig { get; set; }
        public string? ExtraConfig { get; set; }

        public static MCPEndpointDto FromEntity(MCPEndpoint entity)
        {
            return new MCPEndpointDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Endpoint = entity.Endpoint,
                EndpointType = entity.EndpointType,
                ProtocolVersion = entity.ProtocolVersion,
                Description = entity.Description,
                Enabled = entity.Enabled,
                LastTestedTime = entity.LastTestedTime,
                LastTestResult = entity.LastTestResult,
                LastToolCount = entity.LastToolCount,
                AuthConfig = entity.AuthConfig,
                ExtraConfig = entity.ExtraConfig
            };
        }
    }

    /// <summary>
    /// createoredit MCP Endpoint request
    /// </summary>
    public class MCPEndpointCreateOrEditRequest
    {
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.IdCreate")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Address")]
        public string Endpoint { get; set; }

        [MaxLength(50)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Type")]
        public string EndpointType { get; set; }

        [MaxLength(20)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.ProtocolVersion")]
        public string ProtocolVersion { get; set; }

        [MaxLength(500)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Description")]
        public string Description { get; set; }

        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Enabled")]
        public bool Enabled { get; set; } = true;

        [MaxLength(1000)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.AuthConfig")]
        public string AuthConfig { get; set; }

        [MaxLength(2000)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.ExtraConfig")]
        public string ExtraConfig { get; set; }
    }

    /// <summary>
    /// delete MCP Endpoint request
    /// </summary>
    public class MCPEndpointDeleteRequest
    {
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.IdDelete")]
        public int Id { get; set; }
    }

    /// <summary>
    /// test MCP Endpoint request
    /// </summary>
    public class MCPEndpointTestRequest
    {
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.IdTest")]
        public int Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class MCPEndpointTestConnectionRequest
    {
        [MaxLength(100)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Address")]
        public string Endpoint { get; set; }

        [MaxLength(1000)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.AuthConfig")]
        public string AuthConfig { get; set; }

        [MaxLength(50)]
        [LocalizedDescription(typeof(McpResource), "Parameter.MCP.Endpoint.Type")]
        public string? EndpointType { get; set; }
    }
}
