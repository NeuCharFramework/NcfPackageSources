/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：McpConnectionTestService.cs
    文件功能描述：MCP Endpoint 连接测试与工具（Function）发现服务。
    使用 Senparc.AI.AgentKernel 的 McpToolsetBuilder 建立真实连接并读取远端工具列表。


    创建标识：Senparc - 20260924

    修改标识：Senparc - 20260924
    修改描述：v0.5.6 新增基于 Senparc.AI.AgentKernel 的 MCP 连接测试与工具发现

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Senparc.AI.AgentKernel.Mcp;
using Senparc.AI.Interfaces;

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
    /// 使用 <see cref="McpToolsetBuilder"/>（Senparc.AI.AgentKernel）对 MCP Endpoint 发起真实连接，
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
        public async Task<McpConnectionTestResult> TestAsync(
            string name,
            string endpointUrl,
            string? bearerToken = null,
            int timeoutSeconds = 15)
        {
            timeoutSeconds = Math.Max(3, Math.Min(120, timeoutSeconds));

            try
            {
                if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri))
                {
                    return Fail(400, $"端点地址不是有效的绝对 URL：{endpointUrl}");
                }

                var option = new McpServerOption
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "MCP-Test" : name,
                    ServerName = string.IsNullOrWhiteSpace(name) ? "MCP-Test" : name,
                    SseUrl = uri.ToString(),
                    LocalSseUrl = uri.ToString(),
                    RequirePublicUrl = false,
                    AuthorizationBearerToken = bearerToken
                };

                // McpToolsetBuilder.PrepareAsync 内部会：
                // 1) 通过 HttpClientTransport 建立到端点的真实连接
                // 2) 调用 ListToolsAsync 读取远端工具
                // LocalFunctionProxy 模式会保留 RuntimeMcpClient，使用 await using 确保释放
                await using var result = await McpToolsetBuilder.PrepareAsync(option, uri.ToString());

                var tools = (result.DiscoveredMcpTools ?? Array.Empty<ModelContextProtocol.Client.McpClientTool>())
                    .Where(z => z != null)
                    .Select(MapTool)
                    .Where(z => z != null)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(result.ToolDiscoveryError))
                {
                    return new McpConnectionTestResult
                    {
                        Success = false,
                        Status = 500,
                        StatusMessage = $"连接成功，但读取工具列表失败：{result.ToolDiscoveryError}",
                        Tools = tools
                    };
                }

                return new McpConnectionTestResult
                {
                    Success = true,
                    Status = 200,
                    StatusMessage = $"连接成功，发现 {tools.Count} 个工具（Function）",
                    Tools = tools
                };
            }
            catch (OperationCanceledException)
            {
                return Fail(408, $"连接超时（超过 {timeoutSeconds} 秒），请检查端点地址是否可访问");
            }
            catch (Exception ex)
            {
                return Fail(500, $"连接失败：{ex.Message}");
            }
        }

        private static McpToolInfo? MapTool(ModelContextProtocol.Client.McpClientTool tool)
        {
            try
            {
                var info = new McpToolInfo
                {
                    Name = tool.Name,
                    Description = tool.ProtocolTool?.Description ?? tool.Description,
                    Title = tool.Title,
                    InputSchemaJson = tool.JsonSchema.ValueKind == JsonValueKind.Undefined
                        ? null
                        : tool.JsonSchema.GetRawText()
                };

                if (tool.ProtocolTool?.InputSchema.ValueKind == JsonValueKind.Object)
                {
                    info.InputSchemaJson = tool.ProtocolTool.InputSchema.GetRawText();
                }

                info.Parameters = ParseParameters(tool.JsonSchema);
                return info;
            }
            catch
            {
                return new McpToolInfo
                {
                    Name = tool.Name,
                    Description = tool.Description
                };
            }
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
