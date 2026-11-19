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

    public static NeuCharWorkflowChatConfig ParseStored(string json)
    {
        return Normalize(json, null);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

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

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

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
