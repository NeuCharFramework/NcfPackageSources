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
    ///MCP Endpoint Management Service
    /// </summary>
    public class MCPEndpointService : ServiceBase<MCPEndpoint>
    {
        public MCPEndpointService(IRepositoryBase<MCPEndpoint> repo, IServiceProvider serviceProvider) 
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Get all enabled MCP Endpoints
        /// </summary>
        public async Task<List<MCPEndpoint>> GetEnabledEndpointsAsync()
        {
            var endpoints = await this.GetFullListAsync(x => x.Enabled);
            return endpoints?.ToList() ?? new List<MCPEndpoint>();
        }

        /// <summary>
        /// Get the endpoint by name
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByNameAsync(string name)
        {
            return await this.GetObjectAsync(x => x.Name == name);
        }

        /// <summary>
        /// Get the endpoint based on the Endpoint address
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByAddressAsync(string endpoint)
        {
            return await this.GetObjectAsync(x => x.Endpoint == endpoint);
        }

        /// <summary>
        ///Test endpoint connection
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
                // TODO: Implement actual endpoint connection test logic
                // The corresponding test method should be called based on EndpointType.
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
