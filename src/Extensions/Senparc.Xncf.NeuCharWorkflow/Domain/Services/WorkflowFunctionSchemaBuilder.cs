/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowFunctionSchemaBuilder.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261103
    Change description: v0.4.4 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: v0.4.6 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.4.6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Converts runtime Function metadata into the stable schema consumed by the Workflow designer.
/// </summary>
public static class WorkflowFunctionSchemaBuilder
{
    /// <summary>
    /// Builds editable parameter schemas, assigning synthetic names when a malformed descriptor
    /// does not provide a persisted field name.
    /// </summary>
    /// <param name="descriptor">The runtime Function descriptor.</param>
    /// <returns>The ordered parameter schemas.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="descriptor"/> is <see langword="null"/>.</exception>
    public static List<WorkflowFunctionParameterSchema> Build(NeuCharFunctionDescriptor descriptor) =>
        descriptor.Parameters.Select((z, index) =>
        {
            var hasSyntheticName = string.IsNullOrWhiteSpace(z.Name);
            return new WorkflowFunctionParameterSchema
            {
                // Name is the persisted configuration key. A bad third-party Function descriptor
                // must still be editable as a draft, but it cannot be executed until the module
                // restores its real field name.
                Name = hasSyntheticName ? $"parameter_{index + 1}" : z.Name.Trim(),
                Title = z.Title,
                Description = z.Description,
                Required = z.IsRequired,
                ParameterType = (int)z.ParameterType,
                SystemType = z.SystemType,
                MaxLength = z.MaxLength,
                Filterable = z.Filterable,
                AllowCreate = z.AllowCreate,
                AllowMultiple = z.AllowMultiple,
                MaxFiles = z.MaxFiles,
                Accept = z.Accept,
                HasSyntheticName = hasSyntheticName,
                MetadataError = hasSyntheticName
                    ? "Function 参数元数据缺少字段名；当前仅可保存草稿，修复或更新模块后才能运行。"
                    : null,
                DefaultValue = z.ParameterType == ParameterType.Password ? null : z.Value,
                Options = z.SelectionList?.Items?.Select(item => new WorkflowFunctionParameterOption(
                    item.Value,
                    item.Text,
                    item.Note,
                    item.DefaultSelected)).ToList() ?? new List<WorkflowFunctionParameterOption>()
            };
        }).ToList();

    /// <summary>
    /// Builds default parameter values from a set of designer parameter schemas.
    /// </summary>
    /// <param name="parameters">The parameter schemas to inspect.</param>
    /// <returns>A case-insensitive dictionary keyed by persisted parameter name.</returns>
    public static Dictionary<string, object?> BuildDefaults(IEnumerable<WorkflowFunctionParameterSchema> parameters)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters ?? Array.Empty<WorkflowFunctionParameterSchema>())
        {
            object? value = parameter.DefaultValue;
            var options = parameter.Options ?? new List<WorkflowFunctionParameterOption>();
            if (parameter.ParameterType == (int)ParameterType.CheckBoxList)
            {
                value = options.Where(z => z.DefaultSelected).Select(z => z.Value).ToArray();
            }
            else if (parameter.ParameterType == (int)ParameterType.DropDownList && IsBlank(value))
            {
                // Prefer an explicitly selected option. For a required single-option
                // selector, use that option even when metadata omitted its default flag.
                value = options.FirstOrDefault(z => z.DefaultSelected)?.Value;
                if (value == null && parameter.Required && options.Count == 1)
                {
                    value = options[0].Value;
                }
            }
            else if (parameter.ParameterType == (int)ParameterType.CheckBox && value == null)
            {
                value = false;
            }
            else if (parameter.ParameterType == (int)ParameterType.File)
            {
                value = parameter.AllowMultiple ? Array.Empty<string>() : string.Empty;
            }
            result[parameter.Name] = value;
        }
        return result;
    }

    /// <summary>
    /// Adds missing runtime-safe defaults to a persisted parameter JSON object without
    /// overwriting values already supplied by the caller.
    /// </summary>
    /// <param name="parametersJson">The persisted parameter JSON object.</param>
    /// <param name="parameters">The runtime parameter metadata.</param>
    /// <returns>The updated JSON, or the original text when parsing fails.</returns>
    public static string MergeMissingDefaults(string parametersJson, IReadOnlyList<FunctionParameterInfo> parameters)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson);
        }
        catch (JsonException)
        {
            return parametersJson;
        }

        if (parsed is not JsonObject obj)
        {
            return parametersJson;
        }

        var schema = Build(new NeuCharFunctionDescriptor(
            string.Empty,
            string.Empty,
            string.Empty,
            true,
            string.Empty,
            string.Empty,
            string.Empty,
            parameters ?? Array.Empty<FunctionParameterInfo>()));
        foreach (var parameter in schema)
        {
            if (parameter.HasSyntheticName || string.IsNullOrWhiteSpace(parameter.Name) ||
                HasUsableValue(obj, parameter.Name))
            {
                continue;
            }

            if (!BuildDefaults(new[] { parameter }).TryGetValue(parameter.Name, out var fallback) ||
                !HasRuntimeValue(fallback))
            {
                continue;
            }

            RemoveIgnoreCase(obj, parameter.Name);
            obj[parameter.Name] = JsonSerializer.SerializeToNode(fallback);
        }

        return obj.ToJsonString();
    }

    /// <summary>
    /// Determines whether a runtime parameter value is null or blank text.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> when no usable scalar value is present.</returns>
    private static bool IsBlank(object? value) =>
        value == null || (value is string text && string.IsNullOrWhiteSpace(text));

    /// <summary>
    /// Determines whether a value contains usable runtime content, including enumerable items.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> when the value can be passed as a runtime default.</returns>
    private static bool HasRuntimeValue(object? value)
    {
        switch (value)
        {
            case null:
                return false;
            case string text:
                return !string.IsNullOrWhiteSpace(text);
            case System.Collections.IEnumerable enumerable:
                foreach (var unused in enumerable)
                {
                    _ = unused;
                    return true;
                }
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Determines whether a JSON object contains a non-blank value for a property name.
    /// </summary>
    /// <param name="obj">The JSON object to inspect.</param>
    /// <param name="name">The property name to find case-insensitively.</param>
    /// <returns><see langword="true"/> when a usable value exists.</returns>
    private static bool HasUsableValue(JsonObject obj, string name)
    {
        if (!TryGetPropertyIgnoreCase(obj, name, out _, out var node) || node == null)
        {
            return false;
        }

        if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text))
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        return true;
    }

    /// <summary>
    /// Finds a JSON property without regard to case and returns its original key and node.
    /// </summary>
    /// <param name="obj">The JSON object to inspect.</param>
    /// <param name="name">The property name to find.</param>
    /// <param name="key">Receives the original property key.</param>
    /// <param name="node">Receives the property value node.</param>
    /// <returns><see langword="true"/> when a matching property exists.</returns>
    private static bool TryGetPropertyIgnoreCase(JsonObject obj, string name, out string? key, out JsonNode? node)
    {
        foreach (var property in obj)
        {
            if (string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                key = property.Key;
                node = property.Value;
                return true;
            }
        }

        key = null;
        node = null;
        return false;
    }

    /// <summary>
    /// Removes all case-insensitive matches for a property name from a JSON object.
    /// </summary>
    /// <param name="obj">The JSON object to update.</param>
    /// <param name="name">The property name to remove.</param>
    private static void RemoveIgnoreCase(JsonObject obj, string name)
    {
        var keys = obj.Select(property => property.Key)
            .Where(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var key in keys)
        {
            obj.Remove(key);
        }
    }
}

public sealed class WorkflowFunctionParameterSchema
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public int ParameterType { get; set; }
    public string SystemType { get; set; }
    public int MaxLength { get; set; }
    public bool Filterable { get; set; }
    public bool AllowCreate { get; set; }
    public bool AllowMultiple { get; set; }
    public int MaxFiles { get; set; }
    public string Accept { get; set; }
    public bool HasSyntheticName { get; set; }
    public string? MetadataError { get; set; }
    public object? DefaultValue { get; set; }
    public List<WorkflowFunctionParameterOption> Options { get; set; } = new();
}

public sealed record WorkflowFunctionParameterOption(
    string Value,
    string Text,
    string Note,
    bool DefaultSelected);
