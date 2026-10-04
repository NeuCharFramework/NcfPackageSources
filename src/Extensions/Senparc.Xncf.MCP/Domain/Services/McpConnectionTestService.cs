/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：McpConnectionTestService.cs
    文件功能描述：MCP Endpoint 连接测试与工具（Function）发现服务。
    使用 ModelContextProtocol 客户端建立真实连接并读取远端工具列表。


    创建标识：Senparc - 20260924

    修改标识：Senparc - 20260924
    修改描述：v0.5.6 新增基于 Senparc.AI.AgentKernel 的 MCP 连接测试与工具发现

    修改标识：Senparc - 20261003
    修改描述：v0.5.7 按端点类型连接并为初始化及工具发现设置可取消的整体超时

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
    /// MCP 连接测试结果。
    /// </summary>
    public class McpConnectionTestResult
    {
        /// <summary>连接与工具发现是否成功。</summary>
        public bool Success { get; set; }

        /// <summary>状态码（200 成功，非 200 失败）。</summary>
        public int Status { get; set; } = 200;

        /// <summary>状态消息。</summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>工具（Function）数量。</summary>
        public int ToolCount => Tools?.Count ?? 0;

        /// <summary>发现的工具列表。</summary>
        public List<McpToolInfo> Tools { get; set; } = new List<McpToolInfo>();
    }

    /// <summary>
    /// 单个 MCP 工具（Function）信息。
    /// </summary>
    public class McpToolInfo
    {
        /// <summary>工具名称。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>工具标题（可选，来自 Tool/Annotations.Title）。</summary>
        public string? Title { get; set; }

        /// <summary>工具描述。</summary>
        public string? Description { get; set; }

        /// <summary>参数列表。</summary>
        public List<McpToolParameterInfo> Parameters { get; set; } = new List<McpToolParameterInfo>();

        /// <summary>原始输入 JSON Schema（用于“展开”查看）。</summary>
        public string? InputSchemaJson { get; set; }
    }

    /// <summary>
    /// MCP 工具参数信息。
    /// </summary>
    public class McpToolParameterInfo
    {
        /// <summary>参数名。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>参数类型（来自 JSON Schema）。</summary>
        public string? Type { get; set; }

        /// <summary>参数描述。</summary>
        public string? Description { get; set; }

        /// <summary>是否必填。</summary>
        public bool Required { get; set; }
    }

    /// <summary>
    /// 使用 <see cref="McpClient"/> 对 MCP Endpoint 发起真实连接，
    /// 并读取远端返回的工具（Function）列表。
    /// </summary>
    public class McpConnectionTestService
    {
        /// <summary>
        /// 测试 MCP Endpoint 连接并发现工具。
        /// </summary>
        /// <param name="name">端点名称（用于标识）。</param>
        /// <param name="endpointUrl">端点 URI。</param>
        /// <param name="bearerToken">可选 Bearer Token。</param>
        /// <param name="timeoutSeconds">整体超时秒数。</param>
        /// <param name="endpointType">端点类型；未指定时自动检测 HTTP 传输。</param>
        /// <param name="cancellationToken">请求取消信号。</param>
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
        /// 从工具输入 JSON Schema 中解析出参数列表。
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
