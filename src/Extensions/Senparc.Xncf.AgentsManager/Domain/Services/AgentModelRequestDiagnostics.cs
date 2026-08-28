/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentModelRequestDiagnostics.cs
    文件功能描述：Agent 模型请求失败的脱敏诊断

    创建标识：Senparc - 20260827

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

internal static class AgentModelRequestDiagnostics
{
    private const int MaxProviderErrorLength = 1200;

    public static string DescribeBuild(
        AgentTemplate template,
        AgentTemplateRunnerBuildResult build)
    {
        var tools = build?.AgentOptions?.ChatOptions?.Tools?
            .Select(tool => tool?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray()
            ?? Array.Empty<string>();

        var diagnostics = build?.Diagnostics;
        return $"Agent={template?.Id}; Name={template?.Name}; " +
               $"FunctionCallsEnabled={diagnostics?.FunctionCallsEnabled}; " +
               $"ToolCount={diagnostics?.ToolCount}; " +
               $"Tools={(tools.Length == 0 ? "none" : string.Join(",", tools))}; " +
               $"Model={diagnostics?.ModelDescription}; " +
               $"Parameters={diagnostics?.ExecutionParameters}";
    }

    public static async Task<string> DescribeFailureAsync(
        Exception exception,
        IEnumerable<AgentTemplateRunnerBuildResult> builds = null)
    {
        var clientException = FindClientResultException(exception);
        var providerError = clientException == null
            ? "unavailable"
            : await ReadProviderErrorAsync(clientException).ConfigureAwait(false);
        var preparedAgents = builds?
            .Where(build => build != null)
            .Select(build => DescribeBuild(null, build))
            .ToArray()
            ?? Array.Empty<string>();

        return $"ExceptionType={exception?.GetType().FullName}; " +
               $"Status={clientException?.Status.ToString() ?? "unset"}; " +
               $"ProviderError={providerError}; " +
               $"PreparedAgents={(preparedAgents.Length == 0 ? "none" : string.Join(" | ", preparedAgents))}";
    }

    private static ClientResultException FindClientResultException(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is ClientResultException clientException)
            {
                return clientException;
            }
        }

        return null;
    }

    private static async Task<string> ReadProviderErrorAsync(ClientResultException exception)
    {
        try
        {
            var response = exception.GetRawResponse();
            if (response == null)
            {
                return "empty";
            }

            await response.BufferContentAsync().ConfigureAwait(false);
            return ExtractProviderError(response.Content?.ToString());
        }
        catch (Exception readException) when (
            readException is InvalidOperationException
            || readException is System.IO.IOException
            || readException is System.Net.Http.HttpRequestException)
        {
            return $"unavailable({readException.GetType().Name})";
        }
    }

    private static string ExtractProviderError(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "empty";
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var error = root.ValueKind == JsonValueKind.Object
                        && root.TryGetProperty("error", out var nestedError)
                ? nestedError
                : root;

            if (error.ValueKind == JsonValueKind.String)
            {
                return $"message={Sanitize(error.GetString())}";
            }

            if (error.ValueKind == JsonValueKind.Object)
            {
                var fields = new List<string>();
                AddField(fields, error, "code");
                AddField(fields, error, "type");
                AddField(fields, error, "param");
                AddField(fields, error, "message");
                if (fields.Count > 0)
                {
                    return string.Join(", ", fields);
                }
            }

            return $"json-without-standard-error; bytes={System.Text.Encoding.UTF8.GetByteCount(text)}";
        }
        catch (JsonException)
        {
            return $"text={Sanitize(text)}";
        }
    }

    private static void AddField(
        ICollection<string> fields,
        JsonElement error,
        string propertyName)
    {
        if (!error.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Object
                or JsonValueKind.Array
                or JsonValueKind.Null
                or JsonValueKind.Undefined)
        {
            return;
        }

        fields.Add($"{propertyName}={Sanitize(value.ToString())}");
    }

    private static string Sanitize(string text)
    {
        var normalized = Regex.Replace(text ?? string.Empty, "<[^>]+>", " ");
        normalized = Regex.Replace(normalized, @"(?i)\bbearer\s+\S+", "Bearer [redacted]");
        normalized = Regex.Replace(
            normalized,
            @"(?i)(authorization|api[-_ ]?key|token|secret|password)\s*[:=]\s*['""]?[^,;\s'""]+",
            "$1=[redacted]");
        normalized = Regex.Replace(normalized, @"(?i)\bsk-[A-Za-z0-9_-]{8,}\b", "[redacted]");
        normalized = Regex.Replace(normalized, @"https?://\S+", "[url-redacted]");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized.Length <= MaxProviderErrorLength
            ? normalized
            : normalized[..MaxProviderErrorLength] + "...";
    }
}
