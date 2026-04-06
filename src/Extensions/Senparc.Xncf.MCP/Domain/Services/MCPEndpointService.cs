using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 测试端点连接
        /// </summary>
        public async Task<bool> TestEndpointAsync(int endpointId)
        {
            var endpoint = await this.GetObjectAsync(x => x.Id == endpointId);
            if (endpoint == null)
            {
                return false;
            }

            try
            {
                // TODO: 实现实际的端点连接测试逻辑
                // 这里应该根据 EndpointType 调用相应的测试方法
                endpoint.LastTestedTime = DateTime.Now;
                endpoint.LastTestResult = true;
                
                await this.SaveObjectAsync(endpoint);
                return true;
            }
            catch
            {
                endpoint.LastTestedTime = DateTime.Now;
                endpoint.LastTestResult = false;
                await this.SaveObjectAsync(endpoint);
                return false;
            }
        }
    }
}
