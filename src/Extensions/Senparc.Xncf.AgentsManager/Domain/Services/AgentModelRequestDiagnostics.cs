/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentModelRequestDiagnostics.cs
    File description: Related implementation.

    Created by: Senparc - 20260827

    Modified by: Senparc - 20260829
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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

    /// <summary>
    /// Summarizes an Agent build without including prompt or credential content.
    /// </summary>
    /// <param name="template">The Agent template, when available.</param>
    /// <param name="build">The prepared Agent build result.</param>
    /// <returns>A bounded diagnostic summary.</returns>
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

    /// <summary>
    /// Builds a provider-failure diagnostic including status, redacted provider error,
    /// and prepared-Agent summaries.
    /// </summary>
    /// <param name="exception">The exception raised during model execution.</param>
    /// <param name="builds">Optional prepared Agent builds.</param>
    /// <returns>A diagnostic string suitable for logs.</returns>
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

    /// <summary>
    /// Finds the first client-result exception in an inner-exception chain.
    /// </summary>
    /// <param name="exception">The exception chain to inspect.</param>
    /// <returns>The first matching client exception, or <see langword="null"/>.</returns>
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

    /// <summary>
    /// Reads and extracts a bounded provider error from a client response.
    /// </summary>
    /// <param name="exception">The client exception containing the raw response.</param>
    /// <returns>A sanitized provider error summary.</returns>
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

    /// <summary>
    /// Extracts a bounded provider error message from raw response text.
    /// </summary>
    /// <param name="text">The raw provider response text.</param>
    /// <returns>The sanitized provider error text.</returns>
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

    /// <summary>
    /// Adds one scalar provider error field to a diagnostic collection after sanitization.
    /// </summary>
    /// <param name="fields">The destination diagnostic fields.</param>
    /// <param name="error">The provider error JSON object.</param>
    /// <param name="propertyName">The property to read.</param>
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

    /// <summary>
    /// Removes markup, credential-like values, URLs, and excessive text from provider diagnostics.
    /// </summary>
    /// <param name="text">The raw diagnostic text.</param>
    /// <returns>The sanitized bounded diagnostic text.</returns>
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
