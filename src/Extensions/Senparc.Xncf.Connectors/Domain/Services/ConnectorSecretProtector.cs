/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorSecretProtector.cs
    File description: protect and restore connector secrets

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Senparc.Xncf.Connectors.Domain.Services;

public sealed class ConnectorSecretProtector
{
    public const string Prefix = "dp:";
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a data-protection protector for connector secrets.
    /// </summary>
    /// <param name="provider">The ASP.NET Core data-protection provider.</param>
    public ConnectorSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Senparc.Xncf.Connectors.Secrets.v1");
    }

    /// <summary>
    /// Protects a plaintext connector secret and prefixes the value with the storage marker.
    /// </summary>
    /// <param name="plainText">The secret value to protect.</param>
    /// <returns>The prefixed protected secret.</returns>
    public string Protect(string plainText)
        => Prefix + _protector.Protect(plainText);

    /// <summary>
    /// Unprotects a stored prefixed secret; values without the marker are returned unchanged.
    /// </summary>
    /// <param name="stored">The stored or plaintext value.</param>
    /// <returns>The unprotected value or the original unmarked value.</returns>
    public string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        return _protector.Unprotect(stored[Prefix.Length..]);
    }

    /// <summary>
    /// Determines whether a stored value uses the connector secret protection marker.
    /// </summary>
    /// <param name="stored">The stored value to inspect.</param>
    /// <returns><see langword="true"/> when the value starts with <see cref="Prefix"/>.</returns>
    public static bool IsProtected(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);
}

public static class ConnectorConfigurationCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Deserializes connector configuration JSON, returning an empty case-insensitive map for empty or invalid JSON.
    /// </summary>
    /// <param name="json">The configuration JSON.</param>
    /// <returns>The decoded configuration map.</returns>
    public static Dictionary<string, string> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Serializes connector configuration values to JSON.
    /// </summary>
    /// <param name="values">The configuration values to serialize.</param>
    /// <returns>The JSON representation.</returns>
    public static string Write(IReadOnlyDictionary<string, string> values)
        => JsonSerializer.Serialize(values, JsonOptions);

    /// <summary>
    /// Deserializes a JSON string list, returning an empty list for empty or invalid JSON.
    /// </summary>
    /// <param name="json">The JSON list.</param>
    /// <returns>The decoded string list.</returns>
    public static List<string> ReadList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Serializes non-empty distinct values to a JSON string list.
    /// </summary>
    /// <param name="values">The values to normalize and serialize.</param>
    /// <returns>The JSON list representation.</returns>
    public static string WriteList(IEnumerable<string> values)
        => JsonSerializer.Serialize(values.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), JsonOptions);
}
