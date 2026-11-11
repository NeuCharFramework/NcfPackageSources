/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkflowFunctionSchemaBuilder.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

    修改标识：Senparc - 20261103
    修改描述：v0.4.4 add bulk file upload functionality to Sandbox Lab

    修改标识：Senparc - 20261106
    修改描述：执行 Function 时补齐已声明的默认值，并只自动选中明确的唯一选项

    修改标识：Senparc - 20261110
    修改描述：v0.4.6 保留显式传入的空工作流数组，不再把空数组当成未填写

    修改标识：Senparc - 20261111
    修改描述：v0.4.6 保持 Workflow Function 元数据与模块能力兼容

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// 将 XNCF Function 的通用参数元数据转换为 Workflow Designer 可消费的最小 UI 契约。
/// 该契约由 Workflow 模块维护，避免反向依赖 Admin/NeuCharPivot 的页面模型。
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

    public static Dictionary<string, object> BuildDefaults(IEnumerable<WorkflowFunctionParameterSchema> parameters)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters ?? Array.Empty<WorkflowFunctionParameterSchema>())
        {
            object value = parameter.DefaultValue;
            var options = parameter.Options ?? new List<WorkflowFunctionParameterOption>();
            if (parameter.ParameterType == (int)ParameterType.CheckBoxList)
            {
                value = options.Where(z => z.DefaultSelected).Select(z => z.Value).ToArray();
            }
            else if (parameter.ParameterType == (int)ParameterType.DropDownList && IsBlank(value))
            {
                // 只采用明确标记的默认项。必填下拉框仅在唯一选项时自动选中，
                // 避免 Workflow 在多个模型或数据集之间误选第一项。
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
    /// 用 Function 元数据中的默认值和明确选项补齐空参数。已填写的值、模板和上游绑定保持不变。
    /// </summary>
    public static string MergeMissingDefaults(string parametersJson, IReadOnlyList<FunctionParameterInfo> parameters)
    {
        JsonNode parsed;
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

    private static bool IsBlank(object value) =>
        value == null || (value is string text && string.IsNullOrWhiteSpace(text));

    private static bool HasRuntimeValue(object value)
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

    private static bool TryGetPropertyIgnoreCase(JsonObject obj, string name, out string key, out JsonNode node)
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
    public string MetadataError { get; set; }
    public object DefaultValue { get; set; }
    public List<WorkflowFunctionParameterOption> Options { get; set; } = new();
}

public sealed record WorkflowFunctionParameterOption(
    string Value,
    string Text,
    string Note,
    bool DefaultSelected);
