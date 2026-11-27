/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowParameterProtector.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Protects configured workflow function parameters using ASP.NET Core Data Protection
/// and supports preserving existing secret values across partial edits.
/// </summary>
public sealed class NeuCharWorkflowParameterProtector
{
    private const string ProtectedPrefix = "ncp:v1:";
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes the workflow parameter protector with a stable purpose string.
    /// </summary>
    /// <param name="provider">The Data Protection provider.</param>
    public NeuCharWorkflowParameterProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(
            "Senparc.Xncf.NeuCharWorkflow.FunctionParameters.v1");
    }

    /// <summary>
    /// Encrypts non-empty string values whose names are listed as secrets.
    /// </summary>
    /// <param name="parametersJson">The JSON object containing function parameters.</param>
    /// <param name="secretNames">The case-insensitive secret parameter names.</param>
    /// <returns>The JSON object with protected secret values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is not a JSON object.</exception>
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
    /// Decrypts values carrying the module's protected-value prefix.
    /// </summary>
    /// <param name="parametersJson">The protected parameter JSON object.</param>
    /// <returns>The JSON object with decrypted secret values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input is not a JSON object or a protected value cannot be decrypted.</exception>
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
    /// Merges submitted parameters with existing protected values, preserving a stored secret
    /// when the submitted value for that secret is empty or absent.
    /// </summary>
    /// <param name="submittedJson">The new parameter values.</param>
    /// <param name="existingProtectedJson">The previously protected parameter values.</param>
    /// <param name="secretNames">The secret parameter names to preserve.</param>
    /// <returns>The merged JSON object.</returns>
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
    /// Replaces configured secret string values with empty strings for client-facing responses.
    /// </summary>
    /// <param name="protectedJson">The protected parameter JSON object.</param>
    /// <param name="secretNames">The secret parameter names to mask.</param>
    /// <returns>The masked JSON object.</returns>
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
    /// Parses a JSON object and normalizes invalid input into a descriptive domain exception.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed JSON object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not a JSON object.</exception>
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
    /// Finds the existing case-insensitive JSON key matching a requested name.
    /// </summary>
    /// <param name="value">The JSON object to inspect.</param>
    /// <param name="requestedName">The requested property name.</param>
    /// <returns>The existing key or the requested name when absent.</returns>
    private static string FindKey(JsonObject value, string requestedName) =>
        value.Select(z => z.Key).FirstOrDefault(z =>
            string.Equals(z, requestedName, StringComparison.OrdinalIgnoreCase)) ?? requestedName;

    /// <summary>
    /// Trims, removes blank values, and deduplicates protected parameter names.
    /// </summary>
    /// <param name="names">The requested secret names.</param>
    /// <returns>A case-insensitively distinct name collection.</returns>
    private static IReadOnlyCollection<string> NormalizeNames(IEnumerable<string> names) =>
        (names ?? Array.Empty<string>())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
