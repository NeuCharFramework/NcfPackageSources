/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PublishedA2AModelTransport.cs
    文件功能描述：发布型 A2A 模型请求的安全诊断与 API Version 兼容传输

    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// 发布型 A2A 的模型传输上下文。仅记录请求路由和鉴权形态，不记录 API Key、Prompt 或请求正文。
/// </summary>
internal static class PublishedA2AModelTransport
{
    private const int MaxDiagnosticBodyBytes = 256 * 1024;
    private const int MaxDiagnosticTextLength = 480;
    private static readonly AsyncLocal<ModelRequestContext> CurrentContext = new();
    private static readonly ConcurrentDictionary<string, ModelProviderFailure> LatestFailures = new();
    private static readonly HttpClient Client = new(new PublishedA2AModelHttpMessageHandler(), disposeHandler: true);

    public static HttpClient SharedClient => Client;

    public static IDisposable Begin(string diagnosticId, string? apiVersionOverride = null)
    {
        diagnosticId = string.IsNullOrWhiteSpace(diagnosticId) ? "unset" : diagnosticId;
        var previous = CurrentContext.Value;
        if (previous == null || !string.Equals(previous.DiagnosticId, diagnosticId, StringComparison.Ordinal))
        {
            LatestFailures.TryRemove(diagnosticId, out _);
        }

        CurrentContext.Value = new ModelRequestContext(diagnosticId, apiVersionOverride);
        return new ContextScope(previous);
    }

    public static bool TryGetTerminalFailure(string? diagnosticId, out ModelProviderFailure failure)
    {
        failure = null;
        return !string.IsNullOrWhiteSpace(diagnosticId)
               && LatestFailures.TryGetValue(diagnosticId, out failure)
               && failure.IsTerminalConfigurationFailure;
    }

    public static void ClearFailure(string? diagnosticId)
    {
        if (!string.IsNullOrWhiteSpace(diagnosticId))
        {
            LatestFailures.TryRemove(diagnosticId, out _);
        }
    }

    private sealed record ModelRequestContext(string DiagnosticId, string? ApiVersionOverride);

    private sealed class ContextScope : IDisposable
    {
        private readonly ModelRequestContext? _previous;
        private bool _disposed;

        public ContextScope(ModelRequestContext? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentContext.Value = _previous;
            _disposed = true;
        }
    }

    private sealed class PublishedA2AModelHttpMessageHandler : DelegatingHandler
    {
        public PublishedA2AModelHttpMessageHandler()
            : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var context = CurrentContext.Value;
            var originalUri = request.RequestUri;
            if (context != null && !string.IsNullOrWhiteSpace(context.ApiVersionOverride))
            {
                request.RequestUri = ReplaceApiVersion(request.RequestUri, context.ApiVersionOverride);
            }

            try
            {
                var requestSummary = context == null
                    ? "unavailable"
                    : await BuildRequestSummaryAsync(request, cancellationToken).ConfigureAwait(false);
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (context != null && !response.IsSuccessStatusCode)
                {
                    var providerError = await ReadAndRestoreProviderErrorAsync(response, cancellationToken)
                        .ConfigureAwait(false);
                    LatestFailures[context.DiagnosticId] = new ModelProviderFailure(
                        (int)response.StatusCode,
                        providerError,
                        IsTerminalConfigurationFailure(providerError));
                    SenparcTrace.SendCustomLog(
                        "AgentsManager.A2A.ModelProviderResponse",
                        $"DiagnosticId={context.DiagnosticId}; Status={(int)response.StatusCode}; " +
                        $"Route={GetSafeRoute(request.RequestUri)}; ApiVersion={GetApiVersion(request.RequestUri)}; " +
                        $"ApiVersionOverrideApplied={!string.IsNullOrWhiteSpace(context.ApiVersionOverride)}; " +
                        $"Auth={GetAuthenticationShape(request)}; ProviderRequestId={GetProviderRequestId(response)}; " +
                        $"Request={requestSummary}; ProviderError={providerError}");
                }

                return response;
            }
            catch (Exception ex) when (context != null)
            {
                SenparcTrace.SendCustomLog(
                    "AgentsManager.A2A.ModelProviderTransportFailure",
                    $"DiagnosticId={context.DiagnosticId}; ExceptionType={ex.GetType().FullName}; " +
                    $"Route={GetSafeRoute(request.RequestUri ?? originalUri)}; " +
                    $"ApiVersion={GetApiVersion(request.RequestUri ?? originalUri)}; " +
                    $"ApiVersionOverrideApplied={!string.IsNullOrWhiteSpace(context.ApiVersionOverride)}; " +
                    $"Auth={GetAuthenticationShape(request)}; Failure={Summarize(ex.Message)}");
                throw;
            }
        }

        private static async Task<string> BuildRequestSummaryAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var contentType = request.Content?.Headers.ContentType?.MediaType ?? "unset";
            var contentLength = request.Content?.Headers.ContentLength;
            if (request.Content == null)
            {
                return $"contentType={contentType}; bytes=0; messages=0; roles=none; tools=0; stream=unset";
            }

            try
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                contentLength ??= bytes.LongLength;
                if (bytes.Length == 0 || bytes.Length > MaxDiagnosticBodyBytes)
                {
                    return $"contentType={contentType}; bytes={contentLength}; bodyShape=not-inspected";
                }

                using var document = JsonDocument.Parse(bytes);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return $"contentType={contentType}; bytes={contentLength}; bodyShape={root.ValueKind}";
                }

                var messageCount = 0;
                var roles = new List<string>();
                if (root.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
                {
                    messageCount = messages.GetArrayLength();
                    foreach (var message in messages.EnumerateArray())
                    {
                        if (message.ValueKind == JsonValueKind.Object
                            && message.TryGetProperty("role", out var role)
                            && role.ValueKind == JsonValueKind.String)
                        {
                            var roleText = role.GetString();
                            if (!string.IsNullOrWhiteSpace(roleText) && !roles.Contains(roleText, StringComparer.OrdinalIgnoreCase))
                            {
                                roles.Add(roleText);
                            }
                        }
                    }
                }

                var toolCount = root.TryGetProperty("tools", out var tools) && tools.ValueKind == JsonValueKind.Array
                    ? tools.GetArrayLength()
                    : 0;
                var stream = GetSafeScalar(root, "stream");
                var maxTokens = GetSafeScalar(root, "max_tokens");
                var maxCompletionTokens = GetSafeScalar(root, "max_completion_tokens");
                var temperature = GetSafeScalar(root, "temperature");
                var topP = GetSafeScalar(root, "top_p");

                return $"contentType={contentType}; bytes={contentLength}; messages={messageCount}; " +
                       $"roles={(roles.Count == 0 ? "none" : string.Join(',', roles))}; tools={toolCount}; " +
                       $"stream={stream}; maxTokens={maxTokens}; maxCompletionTokens={maxCompletionTokens}; " +
                       $"temperature={temperature}; topP={topP}";
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or HttpRequestException or IOException)
            {
                return $"contentType={contentType}; bytes={(contentLength?.ToString() ?? "unset")}; " +
                       $"bodyShape=unavailable({ex.GetType().Name})";
            }
        }

        private static async Task<string> ReadAndRestoreProviderErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.Content == null)
            {
                return "empty";
            }

            var originalContent = response.Content;
            var originalHeaders = originalContent.Headers
                .Select(header => new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value.ToArray()))
                .ToList();

            byte[] bytes;
            try
            {
                bytes = await originalContent.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException or InvalidOperationException)
            {
                return $"unavailable({ex.GetType().Name})";
            }

            var replacement = new ByteArrayContent(bytes);
            foreach (var header in originalHeaders)
            {
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Content = replacement;
            originalContent.Dispose();

            if (bytes.Length == 0)
            {
                return "empty";
            }

            if (bytes.Length > MaxDiagnosticBodyBytes)
            {
                return $"withheld-too-large({bytes.Length} bytes)";
            }

            return ExtractProviderError(Encoding.UTF8.GetString(bytes));
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
                    return $"message={SanitizeDiagnosticText(error.GetString())}";
                }

                if (error.ValueKind == JsonValueKind.Object)
                {
                    var fields = new List<string>();
                    AddSafeErrorField(fields, error, "code");
                    AddSafeErrorField(fields, error, "type");
                    AddSafeErrorField(fields, error, "param");
                    AddSafeErrorField(fields, error, "message");
                    if (fields.Count > 0)
                    {
                        return string.Join(", ", fields);
                    }
                }

                return $"json-without-standard-error; bytes={Encoding.UTF8.GetByteCount(text)}";
            }
            catch (JsonException)
            {
                return $"text={SanitizeDiagnosticText(text)}";
            }
        }

        private static bool IsTerminalConfigurationFailure(string? providerError)
        {
            if (string.IsNullOrWhiteSpace(providerError))
            {
                return false;
            }

            return providerError.Contains("AI 应用不可用或已暂时停用", StringComparison.OrdinalIgnoreCase)
                   || providerError.Contains("AI应用不可用或已暂时停用", StringComparison.OrdinalIgnoreCase)
                   || providerError.Contains("AI application is unavailable", StringComparison.OrdinalIgnoreCase)
                   || providerError.Contains("AI application has been disabled", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddSafeErrorField(List<string> fields, JsonElement error, string propertyName)
        {
            if (!error.TryGetProperty(propertyName, out var value)
                || value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return;
            }

            fields.Add($"{propertyName}={SanitizeDiagnosticText(value.ToString())}");
        }

        private static string GetSafeScalar(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var value)
                || value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return "unset";
            }

            return Summarize(value.ToString(), 64);
        }

        private static string SanitizeDiagnosticText(string? text)
        {
            var normalized = Regex.Replace(text ?? string.Empty, "<[^>]+>", " ");
            normalized = Regex.Replace(normalized, @"(?i)\bbearer\s+\S+", "Bearer [redacted]");
            normalized = Regex.Replace(
                normalized,
                @"(?i)(authorization|api[-_ ]?key|token|secret)\s*[:=]\s*['""]?[^,;\s'""]+",
                "$1=[redacted]");
            normalized = Regex.Replace(normalized, @"(?i)\bsk-[A-Za-z0-9_-]{8,}\b", "[redacted]");
            normalized = Regex.Replace(normalized, @"https?://\S+", "[url-redacted]");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return Summarize(normalized, MaxDiagnosticTextLength);
        }

        private static Uri? ReplaceApiVersion(Uri? uri, string apiVersion)
        {
            if (uri == null || string.IsNullOrWhiteSpace(apiVersion))
            {
                return uri;
            }

            var queryValues = uri.Query
                .TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(z => !z.StartsWith("api-version=", StringComparison.OrdinalIgnoreCase))
                .ToList();
            queryValues.Add($"api-version={Uri.EscapeDataString(apiVersion)}");

            var builder = new UriBuilder(uri)
            {
                Query = string.Join("&", queryValues)
            };
            return builder.Uri;
        }

        private static string GetSafeRoute(Uri? uri)
        {
            if (uri == null)
            {
                return "unset";
            }

            var path = uri.AbsolutePath;
            var openAiIndex = path.IndexOf("/openai/", StringComparison.OrdinalIgnoreCase);
            if (openAiIndex >= 0)
            {
                // The endpoint prefix can carry an application/developer identifier. Keep the API route
                // and deployment information but hide that prefix from diagnostics.
                path = "/..." + path[openAiIndex..];
            }

            return $"{uri.Host}{path}";
        }

        private static string GetApiVersion(Uri? uri)
        {
            if (uri == null)
            {
                return "unset";
            }

            foreach (var value in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (value.StartsWith("api-version=", StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(value["api-version=".Length..]);
                }
            }

            return "unset";
        }

        private static string GetAuthenticationShape(HttpRequestMessage request)
        {
            if (request.Headers.Contains("api-key"))
            {
                return "api-key";
            }

            return request.Headers.Authorization?.Scheme ?? "none";
        }

        private static string GetProviderRequestId(HttpResponseMessage response)
        {
            var headerNames = new[] { "x-request-id", "x-ms-request-id", "request-id" };
            foreach (var headerName in headerNames)
            {
                if (response.Headers.TryGetValues(headerName, out var values))
                {
                    return values.FirstOrDefault() ?? "unset";
                }
            }

            return "unset";
        }

        private static string Summarize(string? text, int maxLength = 240)
        {
            var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }
    }

    internal sealed record ModelProviderFailure(
        int StatusCode,
        string ProviderError,
        bool IsTerminalConfigurationFailure)
    {
        public string ClientMessage => IsTerminalConfigurationFailure
            ? "AI 应用不可用或已暂时停用。请在 NeuChar 开发者后台启用对应 AI 应用，或为当前 AIModel 更新有效的 DeveloperId、Endpoint 与 ApiKey。"
            : "上游模型服务拒绝了请求。";
    }
}

internal sealed class PublishedA2AModelProviderException : Exception
{
    public PublishedA2AModelTransport.ModelProviderFailure Failure { get; }

    public PublishedA2AModelProviderException(
        PublishedA2AModelTransport.ModelProviderFailure failure,
        Exception innerException)
        : base(failure?.ClientMessage ?? "上游模型服务拒绝了请求。", innerException)
    {
        Failure = failure;
    }
}
