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
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public static class WorkflowFunctionSchemaBuilder
{
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
                //Legacy comment translated to English.
                //Legacy comment translated to English.
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
    ///Legacy comment translated to English.
    /// </summary>
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

    private static bool IsBlank(object? value) =>
        value == null || (value is string text && string.IsNullOrWhiteSpace(text));

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
