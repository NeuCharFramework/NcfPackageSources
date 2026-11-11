/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorSecretProtector.cs
    文件功能描述：保护并还原连接器密钥
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 使用数据保护保存连接器密钥

----------------------------------------------------------------*/

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
