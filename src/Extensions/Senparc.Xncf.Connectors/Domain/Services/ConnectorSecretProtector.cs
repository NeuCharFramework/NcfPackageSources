using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Senparc.Xncf.Connectors.Domain.Services;

public sealed class ConnectorSecretProtector
{
    public const string Prefix = "dp:";
    private readonly IDataProtector _protector;

    public ConnectorSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Senparc.Xncf.Connectors.Secrets.v1");
    }

    public string Protect(string plainText)
        => Prefix + _protector.Protect(plainText);

    public string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        return _protector.Unprotect(stored[Prefix.Length..]);
    }

    public static bool IsProtected(string? stored)
        => !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);
}

public static class ConnectorConfigurationCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public static string Write(IReadOnlyDictionary<string, string> values)
        => JsonSerializer.Serialize(values, JsonOptions);

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

    public static string WriteList(IEnumerable<string> values)
        => JsonSerializer.Serialize(values.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), JsonOptions);
}
