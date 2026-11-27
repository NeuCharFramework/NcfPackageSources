/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharParameterProtector.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class NeuCharParameterProtector
{
    private const string ProtectedPrefix = "ncp:v1:";
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes the versioned Data Protection purpose used for NeuCharPivot
    /// function-parameter secret values.
    /// </summary>
    /// <param name="provider">The application Data Protection provider.</param>
    public NeuCharParameterProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(
            "Senparc.Areas.Admin.NeuCharPivot.FunctionParameters.v1");
    }

    /// <summary>
    /// Protects non-empty string values for the named secret properties while
    /// preserving the surrounding JSON object shape.
    /// </summary>
    /// <param name="parametersJson">The parameter JSON object.</param>
    /// <param name="secretNames">The case-insensitive property names treated as secrets.</param>
    /// <returns>JSON containing protected secret values with the version prefix.</returns>
    public string Protect(string parametersJson, IEnumerable<string> secretNames)
    {
        var parameters = ParseObject(parametersJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var key = FindKey(parameters, name);
            if (parameters[key] is not JsonValue value ||
                !value.TryGetValue<string>(out var plainText) ||
                string.IsNullOrEmpty(plainText))
            {
                continue;
            }
            parameters[key] = ProtectedPrefix + _protector.Protect(plainText);
        }
        return parameters.ToJsonString();
    }

    /// <summary>
    /// Unprotects every value carrying the recognized protected-value prefix.
    /// </summary>
    /// <param name="parametersJson">The protected parameter JSON object.</param>
    /// <returns>JSON with protected values restored to plaintext.</returns>
    /// <exception cref="Exception">Propagates Data Protection failures for invalid or unavailable payloads.</exception>
    public string Unprotect(string parametersJson)
    {
        var parameters = ParseObject(parametersJson);
        foreach (var property in parameters.ToList())
        {
            if (property.Value is JsonValue value &&
                value.TryGetValue<string>(out var storedValue) &&
                storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
            {
                parameters[property.Key] = _protector.Unprotect(storedValue[ProtectedPrefix.Length..]);
            }
        }
        return parameters.ToJsonString();
    }

    /// <summary>
    /// Merges submitted parameters with existing protected secret values when
    /// the submitted object leaves those secret fields empty.
    /// </summary>
    /// <param name="submittedJson">The new parameter JSON object.</param>
    /// <param name="existingProtectedJson">The previously persisted protected object.</param>
    /// <param name="secretNames">The properties eligible for preservation.</param>
    /// <returns>JSON containing submitted values and preserved secret values.</returns>
    public string MergeWithExisting(
        string submittedJson,
        string existingProtectedJson,
        IEnumerable<string> secretNames)
    {
        var submitted = ParseObject(submittedJson);
        var existing = ParseObject(existingProtectedJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var submittedKey = FindKey(submitted, name);
            var existingKey = FindKey(existing, name);
            var hasSubmittedValue = submitted[submittedKey] switch
            {
                JsonObject or JsonArray => true,
                JsonValue submittedValue when submittedValue.TryGetValue<string>(out var text) =>
                    !string.IsNullOrEmpty(text),
                JsonValue => true,
                _ => false
            };
            if (!hasSubmittedValue && existing[existingKey] is JsonValue existingValue &&
                existingValue.TryGetValue<string>(out var storedValue) &&
                storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
            {
                submitted[submittedKey] = _protector.Unprotect(storedValue[ProtectedPrefix.Length..]);
            }
        }
        return submitted.ToJsonString();
    }

    /// <summary>
    /// Replaces secret property values with empty strings for client-side editing
    /// without revealing protected or plaintext content.
    /// </summary>
    /// <param name="protectedJson">The stored parameter JSON object.</param>
    /// <param name="secretNames">The properties to mask.</param>
    /// <returns>JSON safe to return to a client editor.</returns>
    public string MaskForClient(string protectedJson, IEnumerable<string> secretNames)
    {
        var parameters = ParseObject(protectedJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var key = FindKey(parameters, name);
            if (parameters[key] is JsonValue value &&
                value.TryGetValue<string>(out _))
            {
                parameters[key] = string.Empty;
            }
        }
        return parameters.ToJsonString();
    }

    /// <summary>
    /// Parses a JSON object, treating blank input as an empty object.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when input is not a JSON object.</exception>
    private static JsonObject ParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }
        try
        {
            return JsonNode.Parse(json) as JsonObject
                   ?? throw new InvalidOperationException("参数必须是 JSON 对象。");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("参数不是有效的 JSON 对象。");
        }
    }

    /// <summary>
    /// Resolves an existing property key case-insensitively or returns the requested name.
    /// </summary>
    /// <param name="value">The JSON object.</param>
    /// <param name="requestedName">The requested property name.</param>
    /// <returns>The existing matching key or requested name.</returns>
    private static string FindKey(JsonObject value, string requestedName) =>
        value.Select(z => z.Key).FirstOrDefault(z =>
            string.Equals(z, requestedName, StringComparison.OrdinalIgnoreCase)) ?? requestedName;

    /// <summary>
    /// Removes blank secret names and deduplicates the remaining names case-insensitively.
    /// </summary>
    /// <param name="names">The raw secret-name sequence.</param>
    /// <returns>Distinct non-empty secret names.</returns>
    private static IReadOnlyCollection<string> NormalizeNames(IEnumerable<string> names) =>
        (names ?? Array.Empty<string>())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
