/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawMessageContent.cs
    File description: WeixinClawMessageContent.cs implementation and project behavior.

    Created by: Senparc - 20260930

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw media handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

internal static class WeixinClawMessageContent
{
    private const string Prefix = "__NCF_WEIXINCLAW_MEDIA__";

    /// <summary>
    /// Serializes text and stored media metadata into the internal message envelope.
    /// </summary>
    /// <param name="text">The textual message content.</param>
    /// <param name="mediaItems">The stored media items associated with the message.</param>
    /// <returns>The envelope string, or plain text when no media items are present.</returns>
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

    /// <summary>
    /// Parses an internal media envelope, preserving the original value when malformed.
    /// </summary>
    /// <param name="value">The persisted message text.</param>
    /// <returns>A stored message object with a parse-error marker when JSON decoding fails.</returns>
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
    /// <summary>
    /// Gets or sets the textual part of the stored message.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets media records associated with the message.
    /// </summary>
    public List<WeixinClawStoredMedia> MediaItems { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the persisted envelope could not be parsed.
    /// </summary>
    [JsonIgnore]
    public bool ParseError { get; set; }
}

public sealed class WeixinClawStoredMedia
{
    /// <summary>Gets or sets the provider media kind.</summary>
    public string Kind { get; set; }

    /// <summary>Gets or sets the original media name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the media content type.</summary>
    public string ContentType { get; set; }

    /// <summary>Gets or sets the media size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the FileManager or local storage key.</summary>
    public string StorageKey { get; set; }

    /// <summary>Gets or sets the managed FileManager file identifier.</summary>
    public int? FileManagerFileId { get; set; }

    /// <summary>Gets or sets the FileManager error, when import failed.</summary>
    public string FileManagerError { get; set; }

    /// <summary>Gets or sets a media-processing error.</summary>
    public string Error { get; set; }

    /// <summary>Gets or sets the temporary or resolved media URL used by the UI.</summary>
    [JsonIgnore]
    public string Url { get; set; }
}
