/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharParameterProtector.cs
    文件功能描述：NeuChar Loop Task 与 Workflow 敏感参数字段级保护
----------------------------------------------------------------*/

using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Senparc.Areas.Admin.Domain.Services;

/// <summary>
/// 只保护 Function 声明为 Password 的字段。普通参数保持可查询，密文绝不返回给浏览器。
/// </summary>
public sealed class NeuCharParameterProtector
{
    private const string ProtectedPrefix = "ncp:v1:";
    private readonly IDataProtector _protector;

    public NeuCharParameterProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(
            "Senparc.Areas.Admin.NeuCharPivot.FunctionParameters.v1");
    }

    public string Protect(string parametersJson, IEnumerable<string> secretNames)
    {
        var parameters = ParseObject(parametersJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var key = FindKey(parameters, name);
            if (parameters[key] is not JsonValue value ||
                !value.TryGetValue<string>(out var plainText) ||
                string.IsNullOrEmpty(plainText))
            {
                continue;
            }
            parameters[key] = ProtectedPrefix + _protector.Protect(plainText);
        }
        return parameters.ToJsonString();
    }

    public string Unprotect(string parametersJson)
    {
        var parameters = ParseObject(parametersJson);
        foreach (var property in parameters.ToList())
        {
            if (property.Value is JsonValue value &&
                value.TryGetValue<string>(out var storedValue) &&
                storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
            {
                parameters[property.Key] = _protector.Unprotect(storedValue[ProtectedPrefix.Length..]);
            }
        }
        return parameters.ToJsonString();
    }

    public string MergeWithExisting(
        string submittedJson,
        string existingProtectedJson,
        IEnumerable<string> secretNames)
    {
        var submitted = ParseObject(submittedJson);
        var existing = ParseObject(existingProtectedJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var submittedKey = FindKey(submitted, name);
            var existingKey = FindKey(existing, name);
            var hasSubmittedValue = submitted[submittedKey] is JsonValue submittedValue &&
                                    submittedValue.TryGetValue<string>(out var text) &&
                                    !string.IsNullOrEmpty(text);
            if (!hasSubmittedValue && existing[existingKey] is JsonValue existingValue &&
                existingValue.TryGetValue<string>(out var storedValue) &&
                storedValue.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
            {
                submitted[submittedKey] = _protector.Unprotect(storedValue[ProtectedPrefix.Length..]);
            }
        }
        return submitted.ToJsonString();
    }

    public string MaskForClient(string protectedJson, IEnumerable<string> secretNames)
    {
        var parameters = ParseObject(protectedJson);
        foreach (var name in NormalizeNames(secretNames))
        {
            var key = FindKey(parameters, name);
            if (parameters.ContainsKey(key))
            {
                parameters[key] = string.Empty;
            }
        }
        return parameters.ToJsonString();
    }

    private static JsonObject ParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }
        try
        {
            return JsonNode.Parse(json) as JsonObject
                   ?? throw new InvalidOperationException("参数必须是 JSON 对象。");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("参数不是有效的 JSON 对象。");
        }
    }

    private static string FindKey(JsonObject value, string requestedName) =>
        value.Select(z => z.Key).FirstOrDefault(z =>
            string.Equals(z, requestedName, StringComparison.OrdinalIgnoreCase)) ?? requestedName;

    private static IReadOnlyCollection<string> NormalizeNames(IEnumerable<string> names) =>
        (names ?? Array.Empty<string>())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
