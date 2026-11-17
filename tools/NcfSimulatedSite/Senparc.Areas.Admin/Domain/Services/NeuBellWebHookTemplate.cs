/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookTemplate.cs
    File description: Related implementation.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.
    Legacy comment translated to English.

    Created by: Senparc - 20260914

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public static class NeuBellWebHookTemplate
{
    private static readonly JsonSerializerOptions JsonStringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public const string TokenPayload = "payload";
    public const string TokenOperation = "operation";
    public const string TokenOperationStatus = "operationStatus";

    private static readonly string[] KnownTokens =
    {
        TokenPayload,
        "kind", "action", "actionName", "actionStatus",
        "provider", "providerName", "time",
        TokenOperation, TokenOperationStatus, "moduleUid", "tenantId",
        "id", "title", "summary", "link", "status", "count", "updated",
        "addedCount", "removedCount", "addedTitles", "removedTitles"
    };

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static IReadOnlyList<string> KnownTokenList => KnownTokens;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static string MaskTokens(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf("{{", StringComparison.Ordinal) < 0)
        {
            return text ?? string.Empty;
        }
        return ReplaceTokens(text, _ => "x");
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public static string RenderUrl(string urlTemplate, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrEmpty(urlTemplate))
        {
            return urlTemplate ?? string.Empty;
        }
        return ReplaceTokens(urlTemplate, name =>
        {
            var value = Lookup(tokens, name) ?? string.Empty;
            return Uri.EscapeDataString(value);
        });
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public static string RenderBody(string bodyTemplate, IReadOnlyDictionary<string, string> tokens, out string contentType)
    {
        if (string.IsNullOrEmpty(bodyTemplate))
        {
            contentType = null;
            return string.Empty;
        }

        var jsonMode = IsJsonTemplate(bodyTemplate);
        contentType = jsonMode ? "application/json" : "text/plain; charset=utf-8";
        return ReplaceTokens(bodyTemplate, name =>
        {
            var value = Lookup(tokens, name) ?? string.Empty;
            if (jsonMode && name != TokenPayload)
            {
                //Legacy comment translated to English.
                return JsonSerializer.Serialize(value, JsonStringOptions)[1..^1];
            }
            return value;
        });
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static bool IsJsonTemplate(string bodyTemplate)
    {
        var trimmed = (bodyTemplate ?? string.Empty).TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string ReplaceTokens(
        string text,
        Func<string, string> replacement)
    {
        var result = new System.Text.StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length)
        {
            var start = text.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                result.Append(text, index, text.Length - index);
                break;
            }

            var nameStart = start + 2;
            var nameEnd = nameStart;
            while (nameEnd < text.Length && IsTokenNameChar(text[nameEnd]))
            {
                nameEnd++;
            }

            var name = text.Substring(nameStart, nameEnd - nameStart);
            if (nameEnd + 1 < text.Length && text[nameEnd] == '}' && text[nameEnd + 1] == '}' && IsTokenName(name))
            {
                result.Append(text, index, start - index);
                result.Append(replacement(name));
                index = nameEnd + 2;
                continue;
            }

            result.Append(text, index, start + 2 - index);
            index = start + 2;
        }
        return result.ToString();
    }

    private static bool IsTokenName(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0]))
        {
            return false;
        }
        for (var i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsTokenNameChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static string Lookup(IReadOnlyDictionary<string, string> tokens, string name)
    {
        if (tokens == null)
        {
            return null;
        }
        foreach (var pair in tokens)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }
        return null;
    }
}
