/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: McpConnectionTestService.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260924

    Modified by: Senparc - 20260924
    Change description: v0.5.6 Updated module behavior and related support.

    Modified by: Senparc - 20261003
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.5.10 add comprehensive tests for MCP endpoint management and background tenant scope

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace Senparc.Xncf.MCP.Domain.Services
{
    /// <summary>
    /// Represents the structured result of an MCP endpoint connection test.
    /// </summary>
    public class McpConnectionTestResult
    {
        /// Gets or sets whether endpoint connection and tool discovery succeeded.
        public bool Success { get; set; }

        /// Gets or sets the HTTP or protocol status code observed during testing.
        public int Status { get; set; } = 200;

        /// Gets or sets the diagnostic status message.
        public string StatusMessage { get; set; } = string.Empty;

        /// Gets the number of tools discovered from the endpoint.
        public int ToolCount => Tools?.Count ?? 0;

        /// Gets or sets the discovered tool metadata.
        public List<McpToolInfo> Tools { get; set; } = new List<McpToolInfo>();
    }

    /// <summary>
    /// Describes one tool discovered from an MCP endpoint.
    /// </summary>
    public class McpToolInfo
    {
        /// Gets or sets the tool name.
        public string Name { get; set; } = string.Empty;

        /// Gets or sets the optional display title.
        public string? Title { get; set; }

        /// Gets or sets the optional tool description.
        public string? Description { get; set; }

        /// Gets or sets the discovered input parameters.
        public List<McpToolParameterInfo> Parameters { get; set; } = new List<McpToolParameterInfo>();

        /// Gets or sets the serialized JSON input schema.
        public string? InputSchemaJson { get; set; }
    }

    /// <summary>
    /// Describes one input parameter exposed by an MCP tool.
    /// </summary>
    public class McpToolParameterInfo
    {
        /// Gets or sets the parameter name.
        public string Name { get; set; } = string.Empty;

        /// Gets or sets the JSON-schema type name.
        public string? Type { get; set; }

        /// <summary>parametersdescription。</summary>
        public string? Description { get; set; }

        /// Gets or sets whether the parameter is required.
        public bool Required { get; set; }
    }

    /// <summary>
    /// Tests an HTTP/SSE or streamable-HTTP MCP endpoint and discovers its tools and input schemas.
    /// </summary>
    public class McpConnectionTestService
    {
        /// <summary>
        /// Connects to an MCP endpoint, lists its tools, and converts transport or protocol failures
        /// into a structured result rather than propagating them.
        /// </summary>
        /// <param name="name">The display name used by the MCP transport.</param>
        /// <param name="endpointUrl">The absolute HTTP or HTTPS endpoint URL.</param>
        /// <param name="bearerToken">Optional bearer token sent in the Authorization header.</param>
        /// <param name="timeoutSeconds">Connection timeout, clamped to the range 3 through 120 seconds.</param>
        /// <param name="endpointType">Optional transport type: <c>sse</c>, <c>http</c>, or automatic detection.</param>
        /// <param name="cancellationToken">Token used to cancel the test.</param>
        /// <returns>A result containing status, diagnostic message, and discovered tool metadata.</returns>
        public async Task<McpConnectionTestResult> TestAsync(
            string name,
            string endpointUrl,
            string? bearerToken = null,
            int timeoutSeconds = 15,
            string? endpointType = null,
            CancellationToken cancellationToken = default)
        {
            timeoutSeconds = Math.Max(3, Math.Min(120, timeoutSeconds));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return Fail(400, $"端点地址必须是有效的 HTTP/HTTPS URL：{endpointUrl}");
                }

                var mode = endpointType?.Trim().ToLowerInvariant() switch
                {
                    null or "" => HttpTransportMode.AutoDetect,
                    "sse" => HttpTransportMode.Sse,
                    "http" => HttpTransportMode.StreamableHttp,
                    _ => (HttpTransportMode?)null
                };
                if (mode == null)
                {
                    return Fail(400, $"连接测试不支持端点类型：{endpointType}，请选择 sse 或 http");
                }

                var options = new HttpClientTransportOptions
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "MCP-Test" : name,
                    Endpoint = uri,
                    TransportMode = mode.Value,
                    ConnectionTimeout = TimeSpan.FromSeconds(timeoutSeconds),
                    AdditionalHeaders = string.IsNullOrWhiteSpace(bearerToken)
                        ? null
                        : new Dictionary<string, string> { ["Authorization"] = $"Bearer {bearerToken}" }
                };

                await using var transport = new HttpClientTransport(options);
                await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
                var discoveredTools = await client.ListToolsAsync(cancellationToken: timeout.Token);
                var tools = discoveredTools
                    .Select(MapTool)
                    .ToList();

                return new McpConnectionTestResult
                {
                    Success = true,
                    Status = 200,
                    StatusMessage = $"连接成功，发现 {tools.Count} 个工具（Function）",
                    Tools = tools
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Fail(408, $"连接超时（超过 {timeoutSeconds} 秒），请检查端点地址是否可访问");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Fail(500, $"连接失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Maps an MCP client tool to the module's serializable tool-information contract.
        /// </summary>
        /// <param name="tool">The discovered MCP client tool.</param>
        /// <returns>The mapped tool metadata.</returns>
        private static McpToolInfo MapTool(McpClientTool tool)
        {
            var schema = tool.ProtocolTool?.InputSchema.ValueKind == JsonValueKind.Object
                ? tool.ProtocolTool.InputSchema
                : tool.JsonSchema;
            return new McpToolInfo
            {
                Name = tool.Name,
                Description = tool.ProtocolTool?.Description ?? tool.Description,
                Title = tool.Title,
                InputSchemaJson = schema.ValueKind == JsonValueKind.Undefined ? null : schema.GetRawText(),
                Parameters = ParseParameters(schema)
            };
        }

        /// <summary>
        /// Reads JSON Schema properties and required names into parameter metadata.
        /// </summary>
        /// <param name="schema">The tool input JSON Schema.</param>
        /// <returns>Parameter metadata, or an empty list when the schema has no object properties.</returns>
        private static List<McpToolParameterInfo> ParseParameters(JsonElement schema)
        {
            var parameters = new List<McpToolParameterInfo>();
            if (schema.ValueKind != JsonValueKind.Object)
            {
                return parameters;
            }

            var requiredSet = new HashSet<string>();
            if (schema.TryGetProperty("required", out var requiredEl) && requiredEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in requiredEl.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        requiredSet.Add(item.GetString() ?? string.Empty);
                    }
                }
            }

            if (schema.TryGetProperty("properties", out var propsEl) && propsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in propsEl.EnumerateObject())
                {
                    parameters.Add(new McpToolParameterInfo
                    {
                        Name = prop.Name,
                        Type = prop.Value.ValueKind == JsonValueKind.Object
                            && prop.Value.TryGetProperty("type", out var typeEl)
                            ? typeEl.ToString()
                            : null,
                        Description = prop.Value.ValueKind == JsonValueKind.Object
                            && prop.Value.TryGetProperty("description", out var descEl)
                            ? descEl.GetString()
                            : null,
                        Required = requiredSet.Contains(prop.Name)
                    });
                }
            }

            return parameters;
        }

        /// <summary>
        /// Creates a failed connection-test result without exposing an exception
        /// object to the endpoint caller.
        /// </summary>
        /// <param name="status">The HTTP-style diagnostic status code.</param>
        /// <param name="message">The user-facing failure message.</param>
        /// <returns>A failed <see cref="McpConnectionTestResult"/>.</returns>
        private static McpConnectionTestResult Fail(int status, string message)
        {
            return new McpConnectionTestResult
            {
                Success = false,
                Status = status,
                StatusMessage = message,
                Tools = new List<McpToolInfo>()
            };
        }
    }
}
