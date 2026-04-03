using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.MCP.Domain.Services;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.OHS.Local.AppService
{
    /// <summary>
    ///MCP Endpoint Management AppService
    /// Provides the addition, deletion, modification, checking and testing functions of MCP endpoints
    /// </summary>
    public class MCPEndpointAppService : AppServiceBase
    {
        private readonly MCPEndpointService _mcpEndpointService;

        public MCPEndpointAppService(IServiceProvider serviceProvider, MCPEndpointService mcpEndpointService)
            : base(serviceProvider)
        {
            _mcpEndpointService = mcpEndpointService;
        }

        /// <summary>
        /// Get all MCP Endpoints
        /// </summary>
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetAllEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetFullListAsync(x => true);
                var dtos = endpoints?.Select(e => MCPEndpointDto.FromEntity(e)).ToList() ?? new List<MCPEndpointDto>();
                
                logger.Append($"获取了 {dtos.Count} 个 MCP Endpoints");
                return dtos;
            });
        }

        /// <summary>
        /// Get all enabled MCP Endpoints
        /// </summary>
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetEnabledEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetEnabledEndpointsAsync();
                var dtos = endpoints.Select(e => MCPEndpointDto.FromEntity(e)).ToList();
                
                logger.Append($"获取了 {dtos.Count} 个已启用的 MCP Endpoints");
                return dtos;
            });
        }

        /// <summary>
        ///Create/Edit MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> SaveEndpoint(MCPEndpointCreateOrEditRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return "端点名称不能为空";
                }

                if (string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    return "端点地址不能为空";
                }

                // Check if the name already exists (except when editing)
                if (request.Id == 0)
                {
                    var existing = await _mcpEndpointService.GetEndpointByNameAsync(request.Name);
                    if (existing != null)
                    {
                        return $"端点名称 '{request.Name}' 已存在";
                    }
                }

                MCPEndpoint endpoint;
                if (request.Id > 0)
                {
                    // Edit existing endpoint
                    endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                    if (endpoint == null)
                    {
                        return $"端点 ID {request.Id} 不存在";
                    }
                }
                else
                {
                    // Create new endpoint
                    endpoint = new MCPEndpoint();
                }

                // Update properties
                endpoint.Name = request.Name;
                endpoint.Endpoint = request.Endpoint;
                endpoint.EndpointType = request.EndpointType;
                endpoint.ProtocolVersion = request.ProtocolVersion;
                endpoint.Description = request.Description;
                endpoint.Enabled = request.Enabled;
                endpoint.AuthConfig = request.AuthConfig;
                endpoint.ExtraConfig = request.ExtraConfig;

                await _mcpEndpointService.SaveObjectAsync(endpoint);
                logger.Append($"✓ MCP Endpoint '{endpoint.Name}' 已{(request.Id > 0 ? "更新" : "创建")}");

                return logger.ToString();
            });
        }

        /// <summary>
        /// Delete MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> DeleteEndpoint(MCPEndpointDeleteRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    return "invalid endpoint id";
                }

                var endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                if (endpoint == null)
                {
                    return $"endpoint id {request.Id} not found";
                }

                await _mcpEndpointService.DeleteObjectAsync(endpoint);
                logger.Append($"✓ MCP Endpoint '{endpoint.Name}' 已删除");

                return logger.ToString();
            });
        }

        /// <summary>
        ///Test MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> TestEndpoint(MCPEndpointTestRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    return "无效的端点 ID";
                }

                var result = await _mcpEndpointService.TestEndpointAsync(request.Id);
                
                if (result)
                {
                    logger.Append("✓ 端点连接测试成功");
                }
                else
                {
                    logger.Append("✗ 端点连接测试失败");
                }

                return logger.ToString();
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
                LastTestResult = entity.LastTestResult
            };
        }
    }

    /// <summary>
    ///Create or edit MCP Endpoint requests
    /// </summary>
    public class MCPEndpointCreateOrEditRequest
    {
        [Description("端点 ID||为 0 时表示创建新端点")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Description("端点名称||MCP 端点的名称")]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        [Description("端点地址||MCP 端点的 URI 地址")]
        public string Endpoint { get; set; }

        [MaxLength(50)]
        [Description("端点类型||如 http, sse, stdio 等")]
        public string EndpointType { get; set; }

        [MaxLength(20)]
        [Description("协议版本||MCP 协议版本")]
        public string ProtocolVersion { get; set; }

        [MaxLength(500)]
        [Description("描述||端点的描述信息")]
        public string Description { get; set; }

        [Description("是否启用||是否启用此端点")]
        public bool Enabled { get; set; } = true;

        [MaxLength(1000)]
        [Description("认证配置||认证相关的 JSON 配置")]
        public string AuthConfig { get; set; }

        [MaxLength(2000)]
        [Description("额外配置||其他自定义配置的 JSON")]
        public string ExtraConfig { get; set; }
    }

    /// <summary>
    /// Delete MCP Endpoint request
    /// </summary>
    public class MCPEndpointDeleteRequest
    {
        [Description("端点 ID||要删除的 MCP 端点 ID")]
        public int Id { get; set; }
    }

    /// <summary>
    ///Test MCP Endpoint request
    /// </summary>
    public class MCPEndpointTestRequest
    {
        [Description("端点 ID||要测试的 MCP 端点 ID")]
        public int Id { get; set; }
    }
}
