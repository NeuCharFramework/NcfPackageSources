/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderAiSchemaBuilder.cs
    File description: Related implementation.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260917
    Change description: v0.28.0 Updated module behavior and related support.

    Modified by: Senparc - 20261101
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

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
    /// <summary>
    /// Clones an AI function schema and enriches its parameter schemas with selection or file metadata.
    /// </summary>
    /// <param name="functionSchema">The JSON schema to enrich.</param>
    /// <param name="parameters">The runtime parameter descriptions that drive the enrichment.</param>
    /// <returns>
    /// A cloned schema with matching properties updated; invalid or empty input is returned as a clone
    /// without modification.
    /// </returns>
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
            if (parameter == null || string.IsNullOrWhiteSpace(parameter.Name))
            {
                continue;
            }

            var parameterSchema = FindPropertySchema(root, parameter.Name);
            if (parameterSchema == null)
            {
                continue;
            }

            if (parameter.ParameterType == ParameterType.File)
            {
                ApplyFileMetadata(parameterSchema, parameter);
                continue;
            }

            if (parameter.SelectionList?.Items == null ||
                parameter.SelectionList.Items.Count == 0)
            {
                continue;
            }

            ApplySelectionMetadata(parameterSchema, parameter);
        }

        using var document = JsonDocument.Parse(root.ToJsonString());
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Recursively locates a named property schema in nested JSON schema objects.
    /// </summary>
    /// <param name="schema">The schema object to inspect.</param>
    /// <param name="parameterName">The property name to find, compared case-insensitively.</param>
    /// <returns>The matching property schema, or <see langword="null"/> when absent.</returns>
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

    /// <summary>
    /// Reads the first object-valued <c>properties</c> member from a schema.
    /// </summary>
    /// <param name="schema">The schema object to inspect.</param>
    /// <param name="properties">Receives the property collection when present.</param>
    /// <returns><see langword="true"/> when a usable property collection was found.</returns>
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

    /// <summary>
    /// Adds available selection descriptions and, where appropriate, JSON enum constraints.
    /// </summary>
    /// <param name="parameterSchema">The mutable parameter schema to enrich.</param>
    /// <param name="parameter">The runtime parameter metadata and selection items.</param>
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

    /// <summary>
    /// Adds file-upload type metadata, cardinality, accepted-type details, and file-count limits.
    /// </summary>
    /// <param name="parameterSchema">The mutable parameter schema to enrich.</param>
    /// <param name="parameter">The runtime file-parameter metadata.</param>
    private static void ApplyFileMetadata(
        JsonObject parameterSchema,
        FunctionParameterInfo parameter)
    {
        var allowMultiple = parameter.AllowMultiple || IsArraySchema(parameterSchema);
        if (allowMultiple)
        {
            parameterSchema["type"] = "array";
            parameterSchema["items"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Uploaded Function file ID"
            };
        }
        else
        {
            parameterSchema["type"] = "string";
        }

        parameterSchema["x-ncf-parameter-type"] = "file";
        if (parameter.MaxFiles > 0)
        {
            parameterSchema["maxItems"] = parameter.MaxFiles;
        }

        var details = allowMultiple
            ? "Upload one or more files first and pass their FileId values."
            : "Upload a file first and pass its FileId.";
        if (!string.IsNullOrWhiteSpace(parameter.Accept))
        {
            details += $" Accepted types: {parameter.Accept}.";
        }

        var existingDescription = GetStringProperty(parameterSchema, "description");
        parameterSchema["description"] = string.IsNullOrWhiteSpace(existingDescription)
            ? details
            : $"{existingDescription.Trim()} {details}";
    }

    /// <summary>
    /// Determines whether a schema declares the JSON array type.
    /// </summary>
    /// <param name="schema">The schema to inspect.</param>
    /// <returns><see langword="true"/> when its type is <c>array</c>.</returns>
    private static bool IsArraySchema(JsonObject schema)
    {
        var type = GetStringProperty(schema, "type");
        return string.Equals(type, "array", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a textual selection value to the scalar JSON type declared by a schema.
    /// </summary>
    /// <param name="value">The textual selection value.</param>
    /// <param name="schema">The scalar schema that determines conversion.</param>
    /// <returns>A JSON scalar matching the declared type where conversion succeeds, otherwise a string value.</returns>
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

    /// <summary>
    /// Reads a string-valued property from a JSON schema using a case-insensitive key comparison.
    /// </summary>
    /// <param name="schema">The schema object to inspect.</param>
    /// <param name="propertyName">The property name to locate.</param>
    /// <returns>The string property value, or <see langword="null"/> when absent or non-string.</returns>
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

    /// <summary>
    /// Serializes text as a JSON string literal for inclusion in schema descriptions.
    /// </summary>
    /// <param name="value">The text to quote.</param>
    /// <returns>A JSON-escaped string literal.</returns>
    private static string Quote(string value) =>
        JsonSerializer.Serialize(value ?? string.Empty);
}
