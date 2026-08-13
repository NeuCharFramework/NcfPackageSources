/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PublishedA2AModelTransport.cs
    文件功能描述：发布型 A2A 模型请求的安全诊断与 API Version 兼容传输

    创建标识：Senparc - 20260813
----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// 发布型 A2A 的模型传输上下文。仅记录请求路由和鉴权形态，不记录 API Key、Prompt 或请求正文。
/// </summary>
internal static class PublishedA2AModelTransport
{
    private static readonly AsyncLocal<ModelRequestContext> CurrentContext = new();
    private static readonly HttpClient Client = new(new PublishedA2AModelHttpMessageHandler(), disposeHandler: true);

    public static HttpClient SharedClient => Client;

    public static IDisposable Begin(string diagnosticId, string? apiVersionOverride = null)
    {
        var previous = CurrentContext.Value;
        CurrentContext.Value = new ModelRequestContext(diagnosticId, apiVersionOverride);
        return new ContextScope(previous);
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
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (context != null && !response.IsSuccessStatusCode)
                {
                    SenparcTrace.SendCustomLog(
                        "AgentsManager.A2A.ModelProviderResponse",
                        $"DiagnosticId={context.DiagnosticId}; Status={(int)response.StatusCode}; " +
                        $"Route={GetSafeRoute(request.RequestUri)}; ApiVersion={GetApiVersion(request.RequestUri)}; " +
                        $"ApiVersionOverrideApplied={!string.IsNullOrWhiteSpace(context.ApiVersionOverride)}; " +
                        $"Auth={GetAuthenticationShape(request)}; ProviderRequestId={GetProviderRequestId(response)}");
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

        private static string Summarize(string? text)
        {
            var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= 240 ? normalized : normalized[..240];
        }
    }
}
