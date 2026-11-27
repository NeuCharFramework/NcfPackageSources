/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpConnectorContracts.cs
    File description: reusable MCP endpoint and tool contracts for decoupled modules

    Created by: Senparc - 20261126
    Change description: v0.1.3 expose MCP discovery, secure configuration, and tool invocation

    Modified by: Senparc - 20261127
    Change description: v0.1.2.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.MCP.Abstractions;

/// <summary>
/// Exposes configured MCP endpoints without leaking authentication material.
/// </summary>
public interface IMcpEndpointGateway
{
    Task<IReadOnlyList<McpEndpointDescriptor>> ListEndpointsAsync(
        bool onlyEnabled = true,
        CancellationToken cancellationToken = default);

    Task<McpEndpointDescriptor?> GetEndpointAsync(
        int endpointId,
        CancellationToken cancellationToken = default);

    Task<McpEndpointSaveResult> SaveEndpointAsync(
        McpEndpointSaveRequest request,
        CancellationToken cancellationToken = default);

    Task<McpEndpointTestResult> TestEndpointAsync(
        int endpointId,
        CancellationToken cancellationToken = default);

    Task<McpToolCallResult> CallToolAsync(
        int endpointId,
        string toolName,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken = default);
}

public sealed class McpEndpointDescriptor
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string? EndpointType { get; init; }
    public string? ProtocolVersion { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; }
    public bool AuthConfigured { get; init; }
    public DateTime? LastTestedTime { get; init; }
    public bool? LastTestResult { get; init; }
    public IReadOnlyList<McpToolDescriptor> Tools { get; init; } = Array.Empty<McpToolDescriptor>();
}

public sealed class McpToolDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? InputSchemaJson { get; init; }
    public IReadOnlyList<McpToolParameterDescriptor> Parameters { get; init; } = Array.Empty<McpToolParameterDescriptor>();
}

public sealed class McpToolParameterDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Description { get; init; }
    public bool Required { get; init; }
}

public sealed class McpEndpointSaveRequest
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string? EndpointType { get; init; }
    public string? ProtocolVersion { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
    public string? AuthConfig { get; init; }
    public string? ExtraConfig { get; init; }
}

public sealed record McpEndpointSaveResult(bool Success, string Message, int EndpointId);

public sealed class McpEndpointTestResult
{
    public bool Success { get; init; }
    public int Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<McpToolDescriptor> Tools { get; init; } = Array.Empty<McpToolDescriptor>();
}

public sealed record McpToolCallResult(bool Success, string Output, string? Error = null);
