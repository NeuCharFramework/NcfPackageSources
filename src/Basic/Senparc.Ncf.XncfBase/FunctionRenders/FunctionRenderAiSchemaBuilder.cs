/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionRenderAiSchemaBuilder.cs
    文件功能描述：FunctionRender AI schema 元数据扩展

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Ncf.XncfBase.FunctionRenders;

/// <summary>
/// Adds runtime FunctionRender selection metadata to an AI function schema.
/// </summary>
public static class FunctionRenderAiSchemaBuilder
{
    public static JsonElement ApplySelectionMetadata(
        JsonElement functionSchema,
        IReadOnlyList<FunctionParameterInfo> parameters)
    {
        if (functionSchema.ValueKind != JsonValueKind.Object ||
            parameters == null ||
            parameters.Count == 0)
        {
            return functionSchema.Clone();
        }

        if (JsonNode.Parse(functionSchema.GetRawText()) is not JsonObject root)
        {
            return functionSchema.Clone();
        }

        foreach (var parameter in parameters)
        {
            if (parameter == null ||
                string.IsNullOrWhiteSpace(parameter.Name) ||
                parameter.SelectionList?.Items == null ||
                parameter.SelectionList.Items.Count == 0)
            {
                continue;
            }

            var parameterSchema = FindPropertySchema(root, parameter.Name);
            if (parameterSchema == null)
            {
                continue;
            }

            ApplySelectionMetadata(parameterSchema, parameter);
        }

        using var document = JsonDocument.Parse(root.ToJsonString());
        return document.RootElement.Clone();
    }

    private static JsonObject FindPropertySchema(JsonObject schema, string parameterName)
    {
        if (TryGetProperties(schema, out var properties))
        {
            foreach (var property in properties)
            {
                if (string.Equals(property.Key, parameterName, StringComparison.OrdinalIgnoreCase) &&
                    property.Value is JsonObject propertySchema)
                {
                    return propertySchema;
                }
            }

            foreach (var propertySchema in properties.Select(property => property.Value).OfType<JsonObject>())
            {
                var nested = FindPropertySchema(propertySchema, parameterName);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static bool TryGetProperties(JsonObject schema, out JsonObject properties)
    {
        foreach (var property in schema)
        {
            if (string.Equals(property.Key, "properties", StringComparison.OrdinalIgnoreCase) &&
                property.Value is JsonObject propertyObject)
            {
                properties = propertyObject;
                return true;
            }
        }

        properties = null;
        return false;
    }

    private static void ApplySelectionMetadata(
        JsonObject parameterSchema,
        FunctionParameterInfo parameter)
    {
        var items = parameter.SelectionList.Items
            .Where(item => item != null && item.Value != null)
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        var optionDescription = string.Join(
            "; ",
            items.Select(item =>
            {
                var note = string.IsNullOrWhiteSpace(item.Note)
                    ? string.Empty
                    : $", Note={Quote(item.Note)}";
                return $"Value={Quote(item.Value)}, Text={Quote(item.Text)}{note}";
            }));
        var selectionDescription =
            $"Available options (send Value; Text is for semantic matching): {optionDescription}.";
        var existingDescription = GetStringProperty(parameterSchema, "description");
        parameterSchema["description"] = string.IsNullOrWhiteSpace(existingDescription)
            ? selectionDescription
            : $"{existingDescription.Trim()} {selectionDescription}";

        if (parameter.ParameterType == ParameterType.DropDownList &&
            !parameter.AllowCreate)
        {
            parameterSchema["enum"] = new JsonArray(
                items.Select(item => CreateSchemaValue(item.Value, parameterSchema))
                    .ToArray());
        }
        else if (parameter.ParameterType == ParameterType.CheckBoxList &&
                 IsArraySchema(parameterSchema))
        {
            if (parameterSchema["items"] is not JsonObject itemSchema)
            {
                itemSchema = new JsonObject();
                parameterSchema["items"] = itemSchema;
            }

            itemSchema["enum"] = new JsonArray(
                items.Select(item => CreateSchemaValue(item.Value, itemSchema))
                    .ToArray());
        }
    }

    private static bool IsArraySchema(JsonObject schema)
    {
        var type = GetStringProperty(schema, "type");
        return string.Equals(type, "array", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonNode CreateSchemaValue(string value, JsonObject schema)
    {
        var type = GetStringProperty(schema, "type");
        if (string.Equals(type, "integer", StringComparison.OrdinalIgnoreCase) &&
            long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (string.Equals(type, "number", StringComparison.OrdinalIgnoreCase) &&
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return JsonValue.Create(number);
        }

        if (string.Equals(type, "boolean", StringComparison.OrdinalIgnoreCase) &&
            bool.TryParse(value, out var boolean))
        {
            return JsonValue.Create(boolean);
        }

        return JsonValue.Create(value);
    }

    private static string GetStringProperty(JsonObject schema, string propertyName)
    {
        foreach (var property in schema)
        {
            if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value is JsonValue value &&
                value.TryGetValue<string>(out var text))
            {
                return text;
            }
        }

        return null;
    }

    private static string Quote(string value) =>
        JsonSerializer.Serialize(value ?? string.Empty);
}
