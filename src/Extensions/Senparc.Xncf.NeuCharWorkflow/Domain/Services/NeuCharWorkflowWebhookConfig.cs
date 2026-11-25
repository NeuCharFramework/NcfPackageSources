/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowWebhookConfig.cs
    File description: Domain service and business workflow implementation

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance Workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Persistent Webhook trigger configuration. The server normalizes it to prevent arbitrary HTTP methods, duplicate parameters, or oversized content from being written by the client.
/// </summary>
public sealed class NeuCharWorkflowWebhookConfig
{
    private static readonly Regex ParameterNameRegex = new("^[A-Za-z_][A-Za-z0-9_.-]{0,63}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [JsonPropertyName("method")]
    public string Method { get; set; } = "any";

    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("parameters")]
    public List<NeuCharWorkflowWebhookParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Parses and normalizes client-supplied Webhook configuration, reusing an existing token when present.
    /// </summary>
    /// <param name="requestedJson">The requested configuration JSON.</param>
    /// <param name="existingJson">Previously stored configuration JSON, if any.</param>
    /// <param name="generateToken">Whether a missing token should be generated.</param>
    /// <returns>A normalized Webhook configuration with bounded, unique parameters.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when JSON, access-key, method, parameter, or size constraints are invalid.
    /// </exception>
    public static NeuCharWorkflowWebhookConfig Normalize(
        string requestedJson,
        string? existingJson = null,
        bool generateToken = true)
    {
        var requested = ParseObject(requestedJson);
        var existing = ParseObject(existingJson);
        var method = NormalizeMethod(GetString(requested, "method"));
        var token = GetString(requested, "token");
        if (string.IsNullOrWhiteSpace(token))
        {
            token = GetString(existing, "token");
        }
        if (string.IsNullOrWhiteSpace(token) && generateToken)
        {
            token = GenerateToken();
        }
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
        {
            throw new InvalidOperationException("The Webhook access key is required and must be at most 256 characters.");
        }

        var parameters = ParseParameters(requested);
        return new NeuCharWorkflowWebhookConfig
        {
            Method = method,
            Token = token.Trim(),
            Parameters = parameters
        };
    }

    /// <summary>
    /// Parses stored configuration without generating a new access token.
    /// </summary>
    /// <param name="json">The stored configuration JSON.</param>
    /// <returns>The normalized stored configuration.</returns>
    public static NeuCharWorkflowWebhookConfig ParseStored(string json)
    {
        var config = Normalize(json, null, generateToken: false);
        return config;
    }

    /// <summary>
    /// Serializes the normalized configuration for persistent storage.
    /// </summary>
    /// <returns>The JSON representation of this configuration.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Determines whether a request method is permitted by this configuration.
    /// </summary>
    /// <param name="method">The incoming HTTP method.</param>
    /// <returns><see langword="true"/> when the configured method is unrestricted or matches.</returns>
    public bool IsMethodAllowed(string method) =>
        string.Equals(Method, "any", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Method, method, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Generates a URL-safe cryptographic access token for a Webhook.
    /// </summary>
    /// <returns>A URL-safe token backed by 32 random bytes.</returns>
    public static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes.ToArray());
    }

    /// <summary>
    /// Parses a JSON object and returns an empty element for blank or non-object input.
    /// </summary>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>A cloned object element, or a default element when no object is present.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the text is malformed JSON.</exception>
    private static JsonElement ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.Clone()
                : default;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Webhook configuration is not valid JSON.", ex);
        }
    }

    /// <summary>
    /// Reads a string property from a JSON object without converting other JSON types.
    /// </summary>
    /// <param name="root">The object element to inspect.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The string value, or <see langword="null"/> when absent or not a string.</returns>
    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// Normalizes supported Webhook method labels to <c>any</c>, <c>get</c>, or <c>post</c>.
    /// </summary>
    /// <param name="value">The requested method label.</param>
    /// <returns>The normalized method.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method is unsupported.</exception>
    private static string NormalizeMethod(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" or "any" or "*" => "any",
        "get" => "get",
        "post" => "post",
        _ => throw new InvalidOperationException("The Webhook method must be GET, POST, or unrestricted.")
    };

    /// <summary>
    /// Parses bounded, unique Webhook parameter definitions from a configuration object.
    /// </summary>
    /// <param name="root">The parsed configuration object.</param>
    /// <returns>The normalized parameter list.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a parameter violates naming, uniqueness, or size rules.</exception>
    private static List<NeuCharWorkflowWebhookParameter> ParseParameters(JsonElement root)
    {
        var result = new List<NeuCharWorkflowWebhookParameter>();
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("parameters", out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in values.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("The Webhook parameter configuration is invalid.");
            }
            var name = item.TryGetProperty("name", out var nameElement) &&
                       nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()?.Trim()
                : null;
            if (string.IsNullOrWhiteSpace(name) || !ParameterNameRegex.IsMatch(name))
            {
                throw new InvalidOperationException("Webhook parameter names must start with a letter or underscore and contain only letters, numbers, underscores, dots, or hyphens.");
            }
            if (result.Any(z => string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Webhook parameter “{name}” is duplicated.");
            }
            var required = item.TryGetProperty("required", out var requiredElement) &&
                           requiredElement.ValueKind == JsonValueKind.True;
            var description = item.TryGetProperty("description", out var descriptionElement) &&
                             descriptionElement.ValueKind == JsonValueKind.String
                ? descriptionElement.GetString()?.Trim()
                : null;
            if (description?.Length > 500)
            {
                throw new InvalidOperationException($"The description for Webhook parameter “{name}” must be at most 500 characters.");
            }
            result.Add(new NeuCharWorkflowWebhookParameter(name, required, description));
            if (result.Count > 50)
            {
                throw new InvalidOperationException("A single Webhook may define at most 50 parameters.");
            }
        }
        return result;
    }
}

public sealed class NeuCharWorkflowWebhookParameter
{
    /// <summary>
    /// Initializes an empty parameter for JSON materialization.
    /// </summary>
    public NeuCharWorkflowWebhookParameter() { }

    /// <summary>
    /// Initializes one normalized Webhook parameter definition.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="required">Whether the parameter is required.</param>
    /// <param name="description">The optional parameter description.</param>
    public NeuCharWorkflowWebhookParameter(string name, bool required, string description)
    {
        Name = name;
        Required = required;
        Description = description;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}
