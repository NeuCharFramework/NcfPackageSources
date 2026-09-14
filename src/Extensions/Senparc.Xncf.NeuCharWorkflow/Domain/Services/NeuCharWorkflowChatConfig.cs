/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowChatConfig.cs
    文件功能描述：Chat 触发器的持久化配置

    创建标识：Senparc - 20260909
    创建描述：v0.4.0 新增 Chat 触发器

----------------------------------------------------------------*/

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>
/// Chat 触发器的持久化配置。配置由服务端规范化，避免前端写入过长的标题或欢迎语。
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
    /// 规范化 Chat 配置：优先使用前端请求值，缺失时沿用已有配置，最后落到默认值。
    /// </summary>
    public static NeuCharWorkflowChatConfig Normalize(string requestedJson, string existingJson = null)
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
            Title = Limit(title, MaxTitleLength, "Chat 标题"),
            Greeting = Limit(greeting, MaxGreetingLength, "欢迎语")
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
            throw new InvalidOperationException($"{label}不能超过 {maxLength} 个字符。");
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
            throw new InvalidOperationException("Chat 配置不是有效的 JSON。", ex);
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
