/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowObservedOutputSchema.cs
    文件功能描述：已完成运行的输出结构观察（不记录原始值）


    创建标识：Senparc - 20260811

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

public sealed record NeuCharWorkflowObservedOutputSchema(
    string NodeId,
    string NodeName,
    string NodeType,
    string Identity,
    string TypeName,
    bool IsArray,
    IReadOnlyList<NeuCharFunctionOutputFieldDescriptor> Fields);

/// <summary>Converts a completed node result to a bounded, value-free schema snapshot.</summary>
public static class NeuCharWorkflowObservedOutputSchemaBuilder
{
    private const int MaxFields = 60;
    private const int MaxDepth = 3;

    public static NeuCharWorkflowObservedOutputSchema Build(NeuCharWorkflowNode node, JsonNode value)
    {
        var root = Describe(value);
        var fields = new List<NeuCharFunctionOutputFieldDescriptor>
        {
            new("$", root.typeName, root.typeName, root.isArray, false, "observed-output")
        };
        AddFields(fields, value, "$", root.isArray, 0);
        return new(node.Id, node.Name ?? node.Type, node.Type, GetIdentity(node),
            root.typeName, root.isArray, fields);
    }
    public static string GetIdentity(NeuCharWorkflowNode node)
    {
        var config = node.Config;
        return string.Join("|", node.Type ?? string.Empty,
            GetConfigText(config, "moduleUid"),
            GetConfigText(config, "functionKey"),
            GetConfigText(config, "providerId"),
            GetConfigText(config, "objectId"));
    }

    private static string GetConfigText(JsonObject config, string key)
    {
        return config?[key] is JsonValue value && value.TryGetValue<string>(out var text)
            ? text ?? string.Empty
            : string.Empty;
    }

    private static void AddFields(
        ICollection<NeuCharFunctionOutputFieldDescriptor> fields,
        JsonNode value,
        string path,
        bool requiresIndex,
        int depth)
    {
        if (value == null || depth >= MaxDepth || fields.Count >= MaxFields)
        {
            return;
        }
        if (value is JsonArray array)
        {
            AddFields(fields, array.FirstOrDefault(item => item != null), path, true, depth + 1);
            return;
        }
        if (value is not JsonObject obj)
        {
            return;
        }
        foreach (var property in obj.Where(pair => !IsSensitive(pair.Key)).Take(MaxFields - fields.Count))
        {
            AddField(fields, property.Key, property.Value, path, requiresIndex, depth);
        }
    }

    private static void AddField(
        ICollection<NeuCharFunctionOutputFieldDescriptor> fields,
        string name,
        JsonNode value,
        string path,
        bool requiresIndex,
        int depth)
    {
        if (fields.Count >= MaxFields || string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            return;
        }
        var child = Describe(value);
        var childPath = $"{path}.{name}";
        fields.Add(new NeuCharFunctionOutputFieldDescriptor(
            childPath, name, child.typeName, child.isArray, requiresIndex, "observed-output"));
        AddFields(fields, value, childPath, requiresIndex || child.isArray, depth + 1);
    }

    private static (string typeName, bool isArray) Describe(JsonNode value)
    {
        if (value is JsonArray array)
        {
            return (Describe(array.FirstOrDefault(item => item != null)).typeName, true);
        }
        if (value is JsonObject)
        {
            return ("object", false);
        }
        if (value == null)
        {
            return ("any", false);
        }
        return DescribeValue(value.ToJsonString());
    }
    private static (string typeName, bool isArray) DescribeValue(string json) =>
        json is "true" or "false" ? ("boolean", false) : json.StartsWith('"') ? ("string", false) :
        decimal.TryParse(json, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ? ("number", false) : ("any", false);

    private static bool IsSensitive(string name)
    {
        if (!string.IsNullOrEmpty(name) && name.EndsWith("key", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return new[] { "password", "secret", "token", "authorization", "cookie", "credential", "connectionstring" }
            .Any(word => name?.Contains(word, StringComparison.OrdinalIgnoreCase) == true);
    }
}
