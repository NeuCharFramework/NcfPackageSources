/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MCPEndpointAppService.cs
    文件功能描述：MCPEndpointAppService 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
    /// MCP Endpoint 管理 AppService
    /// 提供 MCP 端点的增删改查以及测试功能
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
        /// 获取所有 MCP Endpoints
        /// </summary>
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetAllEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetFullListAsync(x => true);
                var dtos = endpoints?.Select(e => MCPEndpointDto.FromEntity(e)).ToList() ?? new List<MCPEndpointDto>();
                
                logger.Append(NcfBuiltInResource.Format("MCP.Endpoint.CountAll", "获取了 {0} 个 MCP Endpoints", dtos.Count));
                return dtos;
            });
        }

        /// <summary>
        /// 获取所有已启用的 MCP Endpoints
        /// </summary>
        public async Task<AppResponseBase<List<MCPEndpointDto>>> GetEnabledEndpoints()
        {
            return await this.GetResponseAsync<List<MCPEndpointDto>>(async (response, logger) =>
            {
                var endpoints = await _mcpEndpointService.GetEnabledEndpointsAsync();
                var dtos = endpoints.Select(e => MCPEndpointDto.FromEntity(e)).ToList();
                
                logger.Append(NcfBuiltInResource.Format("MCP.Endpoint.CountEnabled", "获取了 {0} 个已启用的 MCP Endpoints", dtos.Count));
                return dtos;
            });
        }

        /// <summary>
        /// 创建/编辑 MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> SaveEndpoint(MCPEndpointCreateOrEditRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return NcfBuiltInResource.Get("MCP.Endpoint.NameRequired");
                }

                if (string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    return NcfBuiltInResource.Get("MCP.Endpoint.AddressRequired");
                }

                // 检查名称是否已存在（编辑时除外）
                if (request.Id == 0)
                {
                    var existing = await _mcpEndpointService.GetEndpointByNameAsync(request.Name);
                    if (existing != null)
                    {
                        return NcfBuiltInResource.Format("MCP.Endpoint.NameExists", "端点名称“{0}”已存在", request.Name);
                    }
                }

                MCPEndpoint endpoint;
                if (request.Id > 0)
                {
                    // 编辑现有端点
                    endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                    if (endpoint == null)
                    {
                        return NcfBuiltInResource.Format("MCP.Endpoint.IdNotFound", "端点 ID {0} 不存在", request.Id);
                    }
                }
                else
                {
                    // 创建新端点
                    endpoint = new MCPEndpoint();
                }

                // 更新属性
                endpoint.Name = request.Name;
                endpoint.Endpoint = request.Endpoint;
                endpoint.EndpointType = request.EndpointType;
                endpoint.ProtocolVersion = request.ProtocolVersion;
                endpoint.Description = request.Description;
                endpoint.Enabled = request.Enabled;
                endpoint.AuthConfig = request.AuthConfig;
                endpoint.ExtraConfig = request.ExtraConfig;

                await _mcpEndpointService.SaveObjectAsync(endpoint);
                logger.Append(NcfBuiltInResource.Format(
                    request.Id > 0 ? "MCP.Endpoint.Updated" : "MCP.Endpoint.Created",
                    request.Id > 0 ? "✓ MCP Endpoint“{0}”已更新" : "✓ MCP Endpoint“{0}”已创建",
                    endpoint.Name));

                return logger.ToString();
            });
        }

        /// <summary>
        /// 删除 MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> DeleteEndpoint(MCPEndpointDeleteRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    return NcfBuiltInResource.Get("MCP.Endpoint.InvalidId");
                }

                var endpoint = await _mcpEndpointService.GetObjectAsync(x => x.Id == request.Id);
                if (endpoint == null)
                {
                    return NcfBuiltInResource.Format("MCP.Endpoint.IdNotFound", "端点 ID {0} 不存在", request.Id);
                }

                await _mcpEndpointService.DeleteObjectAsync(endpoint);
                logger.Append(NcfBuiltInResource.Format("MCP.Endpoint.Deleted", "✓ MCP Endpoint“{0}”已删除", endpoint.Name));

                return logger.ToString();
            });
        }

        /// <summary>
        /// 测试 MCP Endpoint
        /// </summary>
        public async Task<StringAppResponse> TestEndpoint(MCPEndpointTestRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request.Id <= 0)
                {
                    return NcfBuiltInResource.Get("MCP.Endpoint.InvalidId");
                }

                var result = await _mcpEndpointService.TestEndpointAsync(request.Id);
                
                if (result)
                {
                    logger.Append(NcfBuiltInResource.Get("MCP.Endpoint.TestSucceeded"));
                }
                else
                {
                    logger.Append(NcfBuiltInResource.Get("MCP.Endpoint.TestFailed"));
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
    /// 创建或编辑 MCP Endpoint 请求
    /// </summary>
    public class MCPEndpointCreateOrEditRequest
    {
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.IdCreate")]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(500)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.Address")]
        public string Endpoint { get; set; }

        [MaxLength(50)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.Type")]
        public string EndpointType { get; set; }

        [MaxLength(20)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.ProtocolVersion")]
        public string ProtocolVersion { get; set; }

        [MaxLength(500)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.Description")]
        public string Description { get; set; }

        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.Enabled")]
        public bool Enabled { get; set; } = true;

        [MaxLength(1000)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.AuthConfig")]
        public string AuthConfig { get; set; }

        [MaxLength(2000)]
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.ExtraConfig")]
        public string ExtraConfig { get; set; }
    }

    /// <summary>
    /// 删除 MCP Endpoint 请求
    /// </summary>
    public class MCPEndpointDeleteRequest
    {
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.IdDelete")]
        public int Id { get; set; }
    }

    /// <summary>
    /// 测试 MCP Endpoint 请求
    /// </summary>
    public class MCPEndpointTestRequest
    {
        [LocalizedDescription(typeof(NcfBuiltInResource), "Parameter.MCP.Endpoint.IdTest")]
        public int Id { get; set; }
    }
}
