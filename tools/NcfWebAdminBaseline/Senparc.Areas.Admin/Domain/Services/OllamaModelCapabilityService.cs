using Microsoft.Extensions.Logging;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.Entities;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services;

public sealed class OllamaModelCapabilityService
{
    public const string HttpClientName = "AdminChat.OllamaCapabilities";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaModelCapabilityService> _logger;
    private readonly ConcurrentDictionary<string, bool> _toolSupportCache = new(StringComparer.OrdinalIgnoreCase);

    public OllamaModelCapabilityService(
        IHttpClientFactory httpClientFactory,
        ILogger<OllamaModelCapabilityService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool?> SupportsToolsAsync(
        SenparcAiSetting setting,
        CancellationToken cancellationToken = default)
    {
        if (setting?.AiPlatform != AiPlatform.Ollama)
        {
            return true;
        }

        var endpoint = setting.OllamaKeys?.Endpoint;
        var model = setting.OllamaKeys?.ModelName?.Chat;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var cacheKey = $"{endpoint.TrimEnd('/')}|{model}";
        if (_toolSupportCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildShowModelUri(endpoint))
            {
                Content = JsonContent.Create(new { model })
            };
            using var response = await _httpClientFactory.CreateClient(HttpClientName)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var document = await JsonDocument
                .ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!document.RootElement.TryGetProperty("capabilities", out var capabilities)
                || capabilities.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var supportsTools = false;
            foreach (var capability in capabilities.EnumerateArray())
            {
                if (capability.ValueKind == JsonValueKind.String
                    && string.Equals(
                        capability.GetString(),
                        "tools",
                        StringComparison.OrdinalIgnoreCase))
                {
                    supportsTools = true;
                    break;
                }
            }

            _toolSupportCache[cacheKey] = supportsTools;
            return supportsTools;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException
            or TaskCanceledException
            or JsonException
            or UriFormatException)
        {
            _logger.LogWarning(
                exception,
                "Unable to inspect Ollama model capabilities. Endpoint={Endpoint}, Model={Model}",
                endpoint,
                model);
            return null;
        }
    }

    internal static Uri BuildShowModelUri(string endpoint)
    {
        var baseUri = new Uri(endpoint, UriKind.Absolute);
        var path = baseUri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^3];
        }
        else if (path.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^4];
        }

        var builder = new UriBuilder(baseUri)
        {
            Path = $"{path.TrimEnd('/')}/api/show",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }
}
