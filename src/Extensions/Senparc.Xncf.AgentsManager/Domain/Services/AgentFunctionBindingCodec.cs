/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentFunctionBindingCodec.cs
    文件功能描述：AgentFunctionBindingCodec.cs 相关实现


    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

----------------------------------------------------------------*/

using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Stores the new binding contract inside the historic FunctionCallNames column.
/// This avoids a destructive migration while keeping old comma-separated plugin values readable.
/// </summary>
public static class AgentFunctionBindingCodec
{
    private const string Prefix = "@agent-bindings:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<AgentFunctionBindingDto> Parse(string storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return Array.Empty<AgentFunctionBindingDto>();
        }

        var text = storedValue.Trim();
        if (!text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(value => new AgentFunctionBindingDto
                {
                    Kind = "plugin",
                    Key = value,
                    Name = value
                })
                .ToList();
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<BindingEnvelope>(
                text[Prefix.Length..],
                JsonOptions);
            return Normalize(envelope?.Bindings);
        }
        catch (JsonException)
        {
            return Array.Empty<AgentFunctionBindingDto>();
        }
    }

    public static string Serialize(
        IEnumerable<AgentFunctionBindingDto> bindings,
        string legacyPluginNames = null)
    {
        var normalized = Normalize(bindings).ToList();
        if (normalized.Count == 0 &&
            string.IsNullOrWhiteSpace(legacyPluginNames))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(legacyPluginNames))
        {
            normalized.AddRange(Parse(legacyPluginNames)
                .Where(binding => string.Equals(binding.Kind, "plugin", StringComparison.OrdinalIgnoreCase)));
            normalized = Normalize(normalized).ToList();
        }

        return Prefix + JsonSerializer.Serialize(
            new BindingEnvelope
            {
                Version = 1,
                Bindings = normalized
            },
            JsonOptions);
    }

    public static string GetLegacyPluginNames(string storedValue)
        => string.Join(
            ",",
            Parse(storedValue)
                .Where(binding => string.Equals(binding.Kind, "plugin", StringComparison.OrdinalIgnoreCase))
                .Select(binding => binding.Key)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<AgentFunctionBindingDto> Normalize(
        IEnumerable<AgentFunctionBindingDto> bindings)
    {
        return (bindings ?? Array.Empty<AgentFunctionBindingDto>())
            .Where(binding => binding != null)
            .Select(binding => new AgentFunctionBindingDto
            {
                Kind = NormalizeKind(binding.Kind),
                Key = binding.Key?.Trim(),
                Name = binding.Name?.Trim(),
                Description = binding.Description?.Trim(),
                ModuleUid = binding.ModuleUid?.Trim(),
                FunctionKey = binding.FunctionKey?.Trim(),
                WorkflowId = binding.WorkflowId
            })
            .Where(binding => !string.IsNullOrWhiteSpace(binding.Key))
            .GroupBy(binding => $"{binding.Kind}:{binding.Key}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public static bool IsWorkflowBinding(AgentFunctionBindingDto binding)
        => string.Equals(binding?.Kind, "workflow", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeKind(string kind)
        => kind?.Trim().ToLowerInvariant() switch
        {
            "function" or "functionrender" => "function",
            "workflow" => "workflow",
            _ => "plugin"
        };

    private sealed class BindingEnvelope
    {
        public int Version { get; set; }
        public List<AgentFunctionBindingDto> Bindings { get; set; } = new();
    }
}
