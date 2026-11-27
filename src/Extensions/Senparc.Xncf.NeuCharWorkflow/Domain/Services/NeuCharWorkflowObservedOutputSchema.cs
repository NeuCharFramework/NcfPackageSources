/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowObservedOutputSchema.cs
    File description: Related implementation.

    Created by: Senparc - 20260811

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Describes the bounded, value-free schema observed from one completed Workflow node.
/// </summary>
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

    /// <summary>
    /// Builds a bounded schema describing the observed output of a workflow node without
    /// retaining actual values or sensitive fields.
    /// </summary>
    /// <param name="node">The workflow node that produced the output.</param>
    /// <param name="value">The observed JSON value.</param>
    /// <returns>The value-free observed-output schema.</returns>
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
    /// <summary>
    /// Builds a stable identity from node type and configured module/function/provider identifiers.
    /// </summary>
    /// <param name="node">The workflow node to identify.</param>
    /// <returns>The pipe-delimited node identity.</returns>
    public static string GetIdentity(NeuCharWorkflowNode node)
    {
        var config = node.Config;
        return string.Join("|", node.Type ?? string.Empty,
            GetConfigText(config, "moduleUid"),
            GetConfigText(config, "functionKey"),
            GetConfigText(config, "providerId"),
            GetConfigText(config, "objectId"));
    }

    /// <summary>
    /// Reads a string-valued configuration property.
    /// </summary>
    /// <param name="config">The node configuration object.</param>
    /// <param name="key">The configuration property name.</param>
    /// <returns>The property text or an empty string when absent.</returns>
    private static string GetConfigText(JsonObject config, string key)
    {
        return config?[key] is JsonValue value && value.TryGetValue<string>(out var text)
            ? text ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Recursively adds bounded object and array fields to the schema.
    /// </summary>
    /// <param name="fields">The destination field collection.</param>
    /// <param name="value">The JSON value being inspected.</param>
    /// <param name="path">The current schema path.</param>
    /// <param name="requiresIndex">Whether array indexing is required for child paths.</param>
    /// <param name="depth">The current recursion depth.</param>
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

    /// <summary>
    /// Adds one field descriptor and recursively inspects its child value.
    /// </summary>
    /// <param name="fields">The destination field collection.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="path">The parent schema path.</param>
    /// <param name="requiresIndex">Whether array indexing is required.</param>
    /// <param name="depth">The current recursion depth.</param>
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

    /// <summary>
    /// Describes a JSON value by coarse type and array shape.
    /// </summary>
    /// <param name="value">The value to describe.</param>
    /// <returns>The type name and array flag.</returns>
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
    /// <summary>
    /// Describes a scalar JSON token by boolean, string, number, or unknown type.
    /// </summary>
    /// <param name="json">The serialized scalar JSON token.</param>
    /// <returns>The scalar type name and a false array flag.</returns>
    private static (string typeName, bool isArray) DescribeValue(string json) =>
        json is "true" or "false" ? ("boolean", false) : json.StartsWith('"') ? ("string", false) :
        decimal.TryParse(json, NumberStyles.Number, CultureInfo.InvariantCulture, out _) ? ("number", false) : ("any", false);

    /// <summary>
    /// Determines whether a property name appears to contain credentials or other sensitive data.
    /// </summary>
    /// <param name="name">The property name to inspect.</param>
    /// <returns><see langword="true"/> when the property should be excluded from the schema.</returns>
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
