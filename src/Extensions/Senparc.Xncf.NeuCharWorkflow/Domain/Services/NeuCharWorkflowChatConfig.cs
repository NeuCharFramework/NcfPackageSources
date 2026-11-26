/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharWorkflowChatConfig.cs
    File description: Persistent configuration for the Chat trigger

    Created by: Senparc - 20260909
    Creation description: v0.4.0 add the Chat trigger

    Modified by: Senparc - 20260915
    Change description: v0.4.0 enhance Chat trigger message persistence and recovery

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Persistent configuration for the Chat trigger. The server normalizes it to prevent
/// overly long titles or greetings from being written by the client.
/// </summary>
public sealed class NeuCharWorkflowChatConfig
{
    public const int MaxTitleLength = 100;
    public const int MaxGreetingLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [JsonPropertyName("allowGuest")]
    public bool AllowGuest { get; set; } = true;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("greeting")]
    public string? Greeting { get; set; }

    /// <summary>
    /// Normalize Chat configuration: prefer the client value, keep the existing value when absent, and finally use the default.
    /// </summary>
    public static NeuCharWorkflowChatConfig Normalize(string requestedJson, string? existingJson = null)
    {
        var requested = ParseObject(requestedJson);
        var existing = ParseObject(existingJson);

        var allowGuest = GetBool(requested, "allowGuest")
            ?? GetBool(existing, "allowGuest")
            ?? true;
        var title = GetString(requested, "title") ?? GetString(existing, "title");
        var greeting = GetString(requested, "greeting") ?? GetString(existing, "greeting");

        return new NeuCharWorkflowChatConfig
        {
            AllowGuest = allowGuest,
            Title = Limit(title, MaxTitleLength, "Chat title"),
            Greeting = Limit(greeting, MaxGreetingLength, "Greeting")
        };
    }

    /// <summary>
    /// Parses persisted chat configuration and applies the same normalization rules used for requests.
    /// </summary>
    /// <param name="json">The stored JSON configuration, or blank text for defaults.</param>
    /// <returns>The normalized chat configuration.</returns>
    public static NeuCharWorkflowChatConfig ParseStored(string json)
    {
        return Normalize(json, null);
    }

    /// <summary>
    /// Serializes this normalized chat configuration for persistence or transport.
    /// </summary>
    /// <returns>The JSON representation of the configuration.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Trims a configuration value and rejects text longer than the configured limit.
    /// </summary>
    /// <param name="value">The requested or stored value.</param>
    /// <param name="maxLength">The maximum permitted length.</param>
    /// <param name="label">The label used in the validation exception.</param>
    /// <returns>The trimmed value, or <see langword="null"/> for blank input.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the trimmed value exceeds <paramref name="maxLength"/>.</exception>
    private static string? Limit(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new InvalidOperationException($"{label} must be at most {maxLength} characters.");
        }
        return trimmed;
    }

    /// <summary>
    /// Parses a JSON object and returns a detached root element for later property reads.
    /// </summary>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>The cloned object root, or an undefined element for blank/non-object input.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the text is not valid JSON.</exception>
    private static JsonElement ParseObject(string json)
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
            throw new InvalidOperationException("Chat configuration is not valid JSON.", ex);
        }
    }

    /// <summary>
    /// Reads a string-valued property from a JSON object.
    /// </summary>
    /// <param name="root">The JSON object root.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The property string, or <see langword="null"/> when absent or non-string.</returns>
    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    /// <summary>
    /// Reads a Boolean property from a JSON object.
    /// </summary>
    /// <param name="root">The JSON object root.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The Boolean value, or <see langword="null"/> when absent or not Boolean.</returns>
    private static bool? GetBool(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind == JsonValueKind.True
                ? true
                : value.ValueKind == JsonValueKind.False
                    ? false
                    : null;
        }
        return null;
    }
}
