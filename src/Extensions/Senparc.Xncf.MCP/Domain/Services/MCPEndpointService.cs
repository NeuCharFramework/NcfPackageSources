/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MCPEndpointService.cs
    文件功能描述：MCPEndpointService 相关实现
    
    
    创建标识：Senparc - 20260327
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.Domain.Services
{
    /// <summary>
    /// MCP Endpoint 管理服务
    /// </summary>
    public class MCPEndpointService : ServiceBase<MCPEndpoint>
    {
        public MCPEndpointService(IRepositoryBase<MCPEndpoint> repo, IServiceProvider serviceProvider) 
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 获取所有已启用的 MCP Endpoints
        /// </summary>
        public async Task<List<MCPEndpoint>> GetEnabledEndpointsAsync()
        {
            var endpoints = await this.GetFullListAsync(x => x.Enabled);
            return endpoints?.ToList() ?? new List<MCPEndpoint>();
        }

        /// <summary>
        /// 根据名称获取端点
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByNameAsync(string name)
        {
            return await this.GetObjectAsync(x => x.Name == name);
        }

        /// <summary>
        /// 根据 Endpoint 地址获取端点
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByAddressAsync(string endpoint)
        {
            return await this.GetObjectAsync(x => x.Endpoint == endpoint);
        }

        /// <summary>
        /// 测试端点连接（真实连接 MCP Server 并读取工具列表）
        /// </summary>
        public async Task<McpConnectionTestResult> TestEndpointAsync(int endpointId)
        {
            var endpoint = await this.GetObjectAsync(x => x.Id == endpointId);
            if (endpoint == null)
            {
                return new McpConnectionTestResult
                {
                    Success = false,
                    Status = 404,
                    StatusMessage = "端点不存在"
                };
            }

            return await TestEndpointCoreAsync(endpoint);
        }

        /// <summary>
        /// 对指定端点执行真实连接测试，并持久化测试记录
        /// </summary>
        public async Task<McpConnectionTestResult> TestEndpointCoreAsync(MCPEndpoint endpoint)
        {
            var tester = ServiceProvider.GetRequiredService<McpConnectionTestService>();
            string? bearerToken = ExtractBearerToken(endpoint.AuthConfig);

            var result = await tester.TestAsync(endpoint.Name, endpoint.Endpoint, bearerToken);

            endpoint.LastTestedTime = DateTime.Now;
            endpoint.LastTestResult = result.Success;
            endpoint.LastToolCount = result.ToolCount;
            endpoint.LastToolsJson = result.Success ? JsonSerializer.Serialize(result.Tools) : null;

            await this.SaveObjectAsync(endpoint);
            return result;
        }

        /// <summary>
        /// 从 AuthConfig JSON 中提取 Bearer Token（约定字段：token / accessToken / apiKey）
        /// </summary>
        internal static string? ExtractBearerToken(string? authConfigJson)
        {
            if (string.IsNullOrWhiteSpace(authConfigJson))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(authConfigJson);
                var root = doc.RootElement;
                foreach (var key in new[] { "token", "accessToken", "bearerToken", "apiKey" })
                {
                    if (root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        var token = value.GetString();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            return token;
                        }
                    }
                }
            }
            catch
            {
                // AuthConfig 不是 JSON 时直接作为 token 使用
                return authConfigJson;
            }

            return null;
        }
    }
}
