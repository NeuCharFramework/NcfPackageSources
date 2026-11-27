/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentFunctionBindingCodec.cs
    File description: AgentFunctionBindingCodec.cs related implementation

    Created by: Senparc - 20260822

    Modified by: Senparc - 20260822
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

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

    /// <summary>
    /// Parses the historic comma-separated plugin format or the prefixed JSON binding envelope.
    /// Malformed JSON and blank input produce an empty binding list.
    /// </summary>
    /// <param name="storedValue">The persisted FunctionCallNames value.</param>
    /// <returns>Normalized, distinct function-binding DTOs.</returns>
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

    /// <summary>
    /// Normalizes bindings and serializes them into the versioned prefixed envelope,
    /// optionally merging legacy plugin names.
    /// </summary>
    /// <param name="bindings">The new binding values to persist.</param>
    /// <param name="legacyPluginNames">Optional historic comma-separated plugin names.</param>
    /// <returns>The prefixed JSON value, or <see langword="null"/> when no binding exists.</returns>
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

    /// <summary>
    /// Extracts plugin binding keys into the historic comma-separated representation.
    /// </summary>
    /// <param name="storedValue">The persisted binding value.</param>
    /// <returns>Distinct plugin keys separated by commas.</returns>
    public static string GetLegacyPluginNames(string storedValue)
        => string.Join(
            ",",
            Parse(storedValue)
                .Where(binding => string.Equals(binding.Kind, "plugin", StringComparison.OrdinalIgnoreCase))
                .Select(binding => binding.Key)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Trims binding fields, normalizes binding kind, removes empty keys,
    /// and deduplicates entries by kind and key.
    /// </summary>
    /// <param name="bindings">The binding values to normalize.</param>
    /// <returns>A non-null distinct normalized binding list.</returns>
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

    /// <summary>
    /// Determines whether a binding targets a workflow rather than a plugin or module function.
    /// </summary>
    /// <param name="binding">The binding to inspect.</param>
    /// <returns><see langword="true"/> when the binding kind is <c>workflow</c>.</returns>
    public static bool IsWorkflowBinding(AgentFunctionBindingDto binding)
        => string.Equals(binding?.Kind, "workflow", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Maps persisted kind aliases to the supported plugin, function, or workflow values.
    /// </summary>
    /// <param name="kind">The persisted kind text.</param>
    /// <returns>The normalized binding kind.</returns>
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
