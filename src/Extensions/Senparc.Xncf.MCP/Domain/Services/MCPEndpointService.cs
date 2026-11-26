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

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

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
    /// Provides persistence and connectivity-test operations for configured MCP endpoints.
    /// </summary>
    public class MCPEndpointService : ServiceBase<MCPEndpoint>
    {
        /// <summary>
        /// Initializes the MCP endpoint service.
        /// </summary>
        /// <param name="repo">The endpoint repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public MCPEndpointService(IRepositoryBase<MCPEndpoint> repo, IServiceProvider serviceProvider) 
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Loads all enabled MCP endpoints.
        /// </summary>
        /// <returns>The enabled endpoint entities, or an empty list when none exist.</returns>
        public async Task<List<MCPEndpoint>> GetEnabledEndpointsAsync()
        {
            var endpoints = await this.GetFullListAsync(x => x.Enabled);
            return endpoints?.ToList() ?? new List<MCPEndpoint>();
        }

        /// <summary>
        /// Loads an MCP endpoint by its exact configured name.
        /// </summary>
        /// <param name="name">The endpoint name.</param>
        /// <returns>The matching endpoint, or <see langword="null"/> when not found.</returns>
        public async Task<MCPEndpoint> GetEndpointByNameAsync(string name)
        {
            return await this.GetObjectAsync(x => x.Name == name);
        }

        /// <summary>
        /// Loads an MCP endpoint by its exact configured address.
        /// </summary>
        /// <param name="endpoint">The endpoint address.</param>
        /// <returns>The matching endpoint, or <see langword="null"/> when not found.</returns>
        public async Task<MCPEndpoint> GetEndpointByAddressAsync(string endpoint)
        {
            return await this.GetObjectAsync(x => x.Endpoint == endpoint);
        }

        /// <summary>
        /// Tests the endpoint identified by the supplied database identifier and persists
        /// the latest test result and discovered tool count.
        /// </summary>
        /// <param name="endpointId">The endpoint identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the network test.</param>
        /// <returns>A structured connection result; a missing endpoint returns status 404.</returns>
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
        /// Tests a supplied MCP endpoint configuration, extracts a bearer token from its
        /// JSON authentication settings, and persists the resulting tool discovery metadata.
        /// </summary>
        /// <param name="endpoint">The endpoint entity to test and update.</param>
        /// <param name="cancellationToken">Token used to cancel the network test.</param>
        /// <returns>The structured connection and tool-discovery result.</returns>
        public async Task<McpConnectionTestResult> TestEndpointCoreAsync(MCPEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            var tester = ServiceProvider.GetRequiredService<McpConnectionTestService>();
            var secrets = ServiceProvider.GetRequiredService<McpEndpointSecretProtector>();
            string? authConfig = secrets.Unprotect(endpoint.AuthConfig);

            var result = await tester.TestAsync(endpoint.Name, endpoint.Endpoint, authConfig,
                endpointType: endpoint.EndpointType, cancellationToken: cancellationToken);

            endpoint.LastTestedTime = DateTime.Now;
            endpoint.LastTestResult = result.Success;
            endpoint.LastToolCount = result.ToolCount;
            endpoint.LastToolsJson = result.Success ? JsonSerializer.Serialize(result.Tools) : null;

            await this.SaveObjectAsync(endpoint);
            return result;
        }

        /// <summary>
        /// Extracts the first non-empty bearer-style token value from supported authentication
        /// JSON property names. Invalid JSON is treated as a legacy raw token value.
        /// </summary>
        /// <param name="authConfigJson">The JSON authentication configuration or legacy raw token.</param>
        /// <returns>The extracted token, or <see langword="null"/> when no token is available.</returns>
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
