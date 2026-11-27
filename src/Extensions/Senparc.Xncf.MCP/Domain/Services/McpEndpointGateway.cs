/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpEndpointGateway.cs
    File description: decoupled MCP endpoint discovery and invocation gateway

    Created by: Senparc - 20261126
    Change description: v0.5.15 provide MCP capabilities to Connectors and future modules

    Modified by: Senparc - 20261127
    Change description: v0.5.14.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.MCP.Abstractions;
using Senparc.Xncf.MCP.Models.DatabaseModel;

namespace Senparc.Xncf.MCP.Domain.Services;

public sealed class McpEndpointGateway : IMcpEndpointGateway
{
    private readonly MCPEndpointService _endpoints;
    private readonly McpConnectionTestService _transport;
    private readonly McpEndpointSecretProtector _secrets;
    private readonly ILogger<McpEndpointGateway> _logger;

    public McpEndpointGateway(
        MCPEndpointService endpoints,
        McpConnectionTestService transport,
        McpEndpointSecretProtector secrets,
        ILogger<McpEndpointGateway> logger)
    {
        _endpoints = endpoints;
        _transport = transport;
        _secrets = secrets;
        _logger = logger;
    }

    public async Task<IReadOnlyList<McpEndpointDescriptor>> ListEndpointsAsync(
        bool onlyEnabled = true,
        CancellationToken cancellationToken = default)
    {
        var endpoints = await _endpoints.GetFullListAsync(
            item => !onlyEnabled || item.Enabled,
            "Name asc");
        return endpoints.Select(MapEndpoint).ToArray();
    }

    public async Task<McpEndpointDescriptor?> GetEndpointAsync(
        int endpointId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = await _endpoints.GetObjectAsync(item => item.Id == endpointId);
        return endpoint == null ? null : MapEndpoint(endpoint);
    }

    public async Task<McpEndpointSaveResult> SaveEndpointAsync(
        McpEndpointSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return new McpEndpointSaveResult(false, "MCP name and endpoint are required.", 0);
        }

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new McpEndpointSaveResult(false, "The MCP endpoint must be a valid HTTP or HTTPS URL.", 0);
        }

        var duplicate = await _endpoints.GetObjectAsync(
            item => item.Name == request.Name.Trim() && item.Id != request.Id);
        if (duplicate != null)
        {
            return new McpEndpointSaveResult(false, $"An MCP endpoint named '{request.Name.Trim()}' already exists.", 0);
        }

        var endpoint = request.Id > 0
            ? await _endpoints.GetObjectAsync(item => item.Id == request.Id)
            : null;
        if (request.Id > 0 && endpoint == null)
        {
            return new McpEndpointSaveResult(false, "The MCP endpoint no longer exists.", 0);
        }

        endpoint ??= new MCPEndpoint();
        var connectionChanged = endpoint.Endpoint != request.Endpoint.Trim()
            || endpoint.EndpointType != (request.EndpointType?.Trim() ?? "http")
            || !string.IsNullOrWhiteSpace(request.AuthConfig);
        var storedAuth = string.IsNullOrWhiteSpace(request.AuthConfig)
            ? endpoint.AuthConfig
            : _secrets.Protect(request.AuthConfig);
        endpoint.Name = request.Name.Trim();
        endpoint.Endpoint = request.Endpoint.Trim();
        endpoint.EndpointType = request.EndpointType?.Trim() ?? "http";
        endpoint.ProtocolVersion = request.ProtocolVersion?.Trim();
        endpoint.Description = request.Description?.Trim();
        endpoint.Enabled = request.Enabled;
        endpoint.AuthConfig = storedAuth;
        endpoint.ExtraConfig = request.ExtraConfig?.Trim();
        if (connectionChanged)
        {
            endpoint.LastTestedTime = null;
            endpoint.LastTestResult = null;
            endpoint.LastToolCount = null;
            endpoint.LastToolsJson = null;
        }
        await _endpoints.SaveObjectAsync(endpoint);

        return new McpEndpointSaveResult(true, "MCP endpoint saved.", endpoint.Id);
    }

    public async Task<McpEndpointTestResult> TestEndpointAsync(
        int endpointId,
        CancellationToken cancellationToken = default)
    {
        var result = await _endpoints.TestEndpointAsync(endpointId, cancellationToken);
        return new McpEndpointTestResult
        {
            Success = result.Success,
            Status = result.Status,
            Message = result.StatusMessage,
            Tools = result.Tools.Select(MapTool).ToArray()
        };
    }

    public async Task<McpToolCallResult> CallToolAsync(
        int endpointId,
        string toolName,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken = default)
    {
        var endpoint = await _endpoints.GetObjectAsync(item => item.Id == endpointId);
        if (endpoint == null || !endpoint.Enabled)
        {
            return new McpToolCallResult(false, string.Empty, "The MCP endpoint does not exist or is disabled.");
        }

        var result = await _transport.CallToolAsync(
            endpoint.Name,
            endpoint.Endpoint,
            _secrets.Unprotect(endpoint.AuthConfig),
            toolName,
            arguments,
            FindToolSchema(endpoint.LastToolsJson, toolName),
            endpoint.EndpointType,
            cancellationToken: cancellationToken);
        return result.Success
            ? new McpToolCallResult(true, result.Output)
            : new McpToolCallResult(false, string.Empty, result.Error);
    }

    private McpEndpointDescriptor MapEndpoint(MCPEndpoint endpoint)
    {
        var tools = new List<McpToolDescriptor>();
        if (!string.IsNullOrWhiteSpace(endpoint.LastToolsJson))
        {
            try
            {
                var stored = JsonSerializer.Deserialize<List<McpToolInfo>>(endpoint.LastToolsJson);
                if (stored != null)
                {
                    tools.AddRange(stored.Select(MapTool));
                }
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Ignoring invalid cached MCP tool metadata for endpoint {EndpointId}",
                    endpoint.Id);
                tools.Clear();
            }
        }

        return new McpEndpointDescriptor
        {
            Id = endpoint.Id,
            Name = endpoint.Name,
            Endpoint = endpoint.Endpoint,
            EndpointType = endpoint.EndpointType,
            ProtocolVersion = endpoint.ProtocolVersion,
            Description = endpoint.Description,
            Enabled = endpoint.Enabled,
            AuthConfigured = !string.IsNullOrWhiteSpace(endpoint.AuthConfig),
            LastTestedTime = endpoint.LastTestedTime,
            LastTestResult = endpoint.LastTestResult,
            Tools = tools
        };
    }

    private static McpToolDescriptor MapTool(McpToolInfo tool) => new()
    {
        Name = tool.Name,
        Title = tool.Title,
        Description = tool.Description,
        InputSchemaJson = tool.InputSchemaJson,
        Parameters = tool.Parameters.Select(parameter => new McpToolParameterDescriptor
        {
            Name = parameter.Name,
            Type = parameter.Type,
            Description = parameter.Description,
            Required = parameter.Required
        }).ToArray()
    };

    private string? FindToolSchema(string? toolsJson, string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<McpToolInfo>>(toolsJson)
                ?.FirstOrDefault(tool => string.Equals(tool.Name, toolName, StringComparison.OrdinalIgnoreCase))
                ?.InputSchemaJson;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Unable to parse cached MCP tool schema for tool {ToolName}", toolName);
            return null;
        }
    }
}
