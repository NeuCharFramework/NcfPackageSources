/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MCPEndpointService.cs
    File description: MCPEndpointService related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261003
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 add comprehensive tests for MCP endpoint management and background tenant scope

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class MCPEndpointService : ServiceBase<MCPEndpoint>
    {
        public MCPEndpointService(IRepositoryBase<MCPEndpoint> repo, IServiceProvider serviceProvider) 
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<List<MCPEndpoint>> GetEnabledEndpointsAsync()
        {
            var endpoints = await this.GetFullListAsync(x => x.Enabled);
            return endpoints?.ToList() ?? new List<MCPEndpoint>();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByNameAsync(string name)
        {
            return await this.GetObjectAsync(x => x.Name == name);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<MCPEndpoint> GetEndpointByAddressAsync(string endpoint)
        {
            return await this.GetObjectAsync(x => x.Endpoint == endpoint);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<McpConnectionTestResult> TestEndpointAsync(int endpointId, CancellationToken cancellationToken = default)
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

            return await TestEndpointCoreAsync(endpoint, cancellationToken);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<McpConnectionTestResult> TestEndpointCoreAsync(MCPEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            var tester = ServiceProvider.GetRequiredService<McpConnectionTestService>();
            string? bearerToken = ExtractBearerToken(endpoint.AuthConfig);

            var result = await tester.TestAsync(endpoint.Name, endpoint.Endpoint, bearerToken,
                endpointType: endpoint.EndpointType, cancellationToken: cancellationToken);

            endpoint.LastTestedTime = DateTime.Now;
            endpoint.LastTestResult = result.Success;
            endpoint.LastToolCount = result.ToolCount;
            endpoint.LastToolsJson = result.Success ? JsonSerializer.Serialize(result.Tools) : null;

            await this.SaveObjectAsync(endpoint);
            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
                return authConfigJson;
            }

            return null;
        }
    }
}
