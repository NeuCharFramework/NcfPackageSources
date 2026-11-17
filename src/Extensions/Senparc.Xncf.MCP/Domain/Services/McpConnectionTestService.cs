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
    ///Legacy comment translated to English.
    /// </summary>
    public class McpConnectionTestResult
    {
        ///Legacy comment translated to English.
        public bool Success { get; set; }

        ///Legacy comment translated to English.
        public int Status { get; set; } = 200;

        ///Legacy comment translated to English.
        public string StatusMessage { get; set; } = string.Empty;

        ///Legacy comment translated to English.
        public int ToolCount => Tools?.Count ?? 0;

        ///Legacy comment translated to English.
        public List<McpToolInfo> Tools { get; set; } = new List<McpToolInfo>();
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class McpToolInfo
    {
        ///Legacy comment translated to English.
        public string Name { get; set; } = string.Empty;

        ///Legacy comment translated to English.
        public string? Title { get; set; }

        ///Legacy comment translated to English.
        public string? Description { get; set; }

        ///Legacy comment translated to English.
        public List<McpToolParameterInfo> Parameters { get; set; } = new List<McpToolParameterInfo>();

        ///Legacy comment translated to English.
        public string? InputSchemaJson { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class McpToolParameterInfo
    {
        ///Legacy comment translated to English.
        public string Name { get; set; } = string.Empty;

        ///Legacy comment translated to English.
        public string? Type { get; set; }

        /// <summary>parametersdescription。</summary>
        public string? Description { get; set; }

        ///Legacy comment translated to English.
        public bool Required { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class McpConnectionTestService
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
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
