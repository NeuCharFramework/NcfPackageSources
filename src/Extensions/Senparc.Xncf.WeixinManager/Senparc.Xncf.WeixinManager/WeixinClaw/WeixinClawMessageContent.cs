using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

internal static class WeixinClawMessageContent
{
    private const string Prefix = "__NCF_WEIXINCLAW_MEDIA__";

    public static string Serialize(string text, IReadOnlyList<WeixinClawStoredMedia> mediaItems)
    {
        if (mediaItems == null || mediaItems.Count == 0)
        {
            return text ?? string.Empty;
        }

        return Prefix + JsonSerializer.Serialize(
            new WeixinClawStoredMessage
            {
                Text = text ?? string.Empty,
                MediaItems = new List<WeixinClawStoredMedia>(mediaItems)
            });
    }

    public static WeixinClawStoredMessage Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return new WeixinClawStoredMessage { Text = value ?? string.Empty };
        }

        try
        {
            return JsonSerializer.Deserialize<WeixinClawStoredMessage>(
                       value[Prefix.Length..],
                       WeixinClawProtocol.JsonOptions)
                   ?? new WeixinClawStoredMessage { Text = value };
        }
        catch
        {
            return new WeixinClawStoredMessage
            {
                Text = value,
                ParseError = true
            };
        }
    }
}

public sealed class WeixinClawStoredMessage
{
    public string Text { get; set; } = string.Empty;

    public List<WeixinClawStoredMedia> MediaItems { get; set; } = new();

    [JsonIgnore]
    public bool ParseError { get; set; }
}

public sealed class WeixinClawStoredMedia
{
    public string Kind { get; set; }

    public string Name { get; set; }

    public string ContentType { get; set; }

    public long Size { get; set; }

    public string StorageKey { get; set; }

    public int? FileManagerFileId { get; set; }

    public string FileManagerError { get; set; }

    public string Error { get; set; }

    [JsonIgnore]
    public string Url { get; set; }
}
