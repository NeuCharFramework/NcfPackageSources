#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningWorkerClient
{
    public const int MaxDatasetBytes = 2 * 1024 * 1024;
    private const long MaxResponseBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex SafeIdentifier = new("^[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}$", RegexOptions.CultureInvariant);
    private readonly HttpClient _http;
    private readonly IFineTuningWorkerConfigurationProvider _configuration;
    private readonly ILogger<FineTuningWorkerClient> _logger;

    [ActivatorUtilitiesConstructor]
    public FineTuningWorkerClient(HttpClient http, IFineTuningWorkerConfigurationProvider configuration,
        ILogger<FineTuningWorkerClient> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    [Obsolete("Use the database-backed worker configuration provider.")]
    public FineTuningWorkerClient(HttpClient http, IOptions<FineTuningWorkerOptions> options,
        ILogger<FineTuningWorkerClient> logger)
        : this(http, new LegacyWorkerConfigurationProvider(options.Value), logger)
    {
    }

    public Task<FineTuningHealth> HealthAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningHealth>(workerAlias, HttpMethod.Get, "health", null, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningHealth> HealthAsync(CancellationToken cancellationToken = default) => HealthAsync("default", cancellationToken);

    public Task<List<FineTuningModel>> GetModelsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningModel>>(workerAlias, HttpMethod.Get, "models", null, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningModel>> GetModelsAsync(CancellationToken cancellationToken = default) => GetModelsAsync("default", cancellationToken);

    public Task<List<FineTuningDataset>> GetDatasetsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningDataset>>(workerAlias, HttpMethod.Get, "datasets", null, cancellationToken);

    public Task<FineTuningCatalogPage<FineTuningDataset>> GetDatasetsPageAsync(string workerAlias, int offset = 0,
        int limit = 100, CancellationToken cancellationToken = default) =>
        GetCatalogAsync<FineTuningDataset>(workerAlias, "datasets", offset, limit, cancellationToken);

    public Task<FineTuningDataset> GetDatasetAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningDataset>(workerAlias, HttpMethod.Get, $"datasets/{Identifier(id)}", null, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningDataset>> GetDatasetsAsync(CancellationToken cancellationToken = default) => GetDatasetsAsync("default", cancellationToken);

    public Task<FineTuningDataset> UploadDatasetAsync(string workerAlias, FineTuningDatasetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        if (Encoding.UTF8.GetByteCount(request.Content) > MaxDatasetBytes)
            throw new NcfExceptionBase("Dataset exceeds the 2 MiB UTF-8 limit.");
        return SendAsync<FineTuningDataset>(workerAlias, HttpMethod.Post, "datasets", request, cancellationToken);
    }
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningDataset> UploadDatasetAsync(FineTuningDatasetRequest request, CancellationToken cancellationToken = default) =>
        UploadDatasetAsync("default", request, cancellationToken);

    public Task<List<FineTuningJob>> GetJobsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningJob>>(workerAlias, HttpMethod.Get, "jobs", null, cancellationToken);

    public Task<FineTuningCatalogPage<FineTuningJob>> GetJobsPageAsync(string workerAlias, int offset = 0,
        int limit = 100, CancellationToken cancellationToken = default) =>
        GetCatalogAsync<FineTuningJob>(workerAlias, "jobs", offset, limit, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningJob>> GetJobsAsync(CancellationToken cancellationToken = default) => GetJobsAsync("default", cancellationToken);

    public Task<FineTuningJob> CreateJobAsync(string workerAlias, FineTuningJobRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        Identifier(request.ModelId);
        Identifier(request.DatasetId);
        if (request.EvalDatasetId != null)
        {
            Identifier(request.EvalDatasetId);
            if (request.EvalDatasetId == request.DatasetId)
                throw new NcfExceptionBase("Evaluation and training datasets must be different.");
        }
        if (request.Method == "qlora" && request.Backend == "cpu")
            throw new NcfExceptionBase("QLoRA is not supported by the CPU backend.");
        return SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, "jobs", request, cancellationToken);
    }
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> CreateJobAsync(FineTuningJobRequest request, CancellationToken cancellationToken = default) =>
        CreateJobAsync("default", request, cancellationToken);

    public Task<FineTuningJob> GetJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}", null, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> GetJobAsync(string id, CancellationToken cancellationToken = default) => GetJobAsync("default", id, cancellationToken);

    public Task<FineTuningJob> CancelJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/cancel", new { }, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> CancelJobAsync(string id, CancellationToken cancellationToken = default) => CancelJobAsync("default", id, cancellationToken);

    public Task<FineTuningEventPage> GetEventsAsync(string workerAlias, string id, long after, int limit, CancellationToken cancellationToken = default)
    {
        if (after < 0 || limit < 1 || limit > 200)
            throw new NcfExceptionBase("Event cursor must be nonnegative and limit must be between 1 and 200.");
        return SendAsync<FineTuningEventPage>(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}/events?after={after}&limit={limit}", null, cancellationToken);
    }
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningEventPage> GetEventsAsync(string id, long after, int limit, CancellationToken cancellationToken = default) =>
        GetEventsAsync("default", id, after, limit, cancellationToken);

    // The caller owns the response and streams the body; model archives are not buffered in Web memory.
    public Task<HttpResponseMessage> DownloadArtifactAsync(string workerAlias, string id, string artifactId, CancellationToken cancellationToken = default) =>
        SendResponseAsync(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}/artifacts/{Identifier(artifactId)}", null, cancellationToken);
    [Obsolete("Select a database worker explicitly.")]
    public Task<HttpResponseMessage> DownloadArtifactAsync(string id, string artifactId, CancellationToken cancellationToken = default) =>
        DownloadArtifactAsync("default", id, artifactId, cancellationToken);

    private sealed class LegacyWorkerConfigurationProvider : IFineTuningWorkerConfigurationProvider
    {
        private readonly FineTuningWorkerConfiguration _configuration;

        public LegacyWorkerConfigurationProvider(FineTuningWorkerOptions options) =>
            _configuration = options.GetLegacyConfiguration();

        public Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default) =>
            workerAlias == "default" ? Task.FromResult(_configuration)
                : throw new NcfExceptionBase("The legacy fine-tuning client only supports the default worker.");

        public Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AIFineTuningWorkerDto>());
    }

    public static string Identifier(string value)
    {
        if (value == null || !SafeIdentifier.IsMatch(value) || value.Contains("..", StringComparison.Ordinal))
            throw new NcfExceptionBase("Invalid fine-tuning resource identifier.");
        return value;
    }

    private static string CatalogPath(string catalog, int offset, int limit)
    {
        if (offset < 0 || offset > 100000 || limit < 1 || limit > 200)
            throw new NcfExceptionBase("Catalog offset must be between 0 and 100000 and limit between 1 and 200.");
        return $"{catalog}/page?offset={offset}&limit={limit}";
    }

    private async Task<FineTuningCatalogPage<T>> GetCatalogAsync<T>(string workerAlias, string catalog, int offset,
        int limit, CancellationToken cancellationToken)
    {
        var page = await SendAsync<FineTuningCatalogPage<T>>(workerAlias, HttpMethod.Get,
            CatalogPath(catalog, offset, limit), null, cancellationToken).ConfigureAwait(false);
        if (page.Items == null || page.Total < 0 || page.Offset != offset || page.Limit != limit
            || page.Items.Count != Math.Min(limit, Math.Max(0, page.Total - offset)))
        {
            _logger.LogError("Invalid fine-tuning catalog page for {Catalog}: offset={Offset}, limit={Limit}", catalog, offset, limit);
            throw new NcfExceptionBase("The fine-tuning worker returned an invalid catalog page.");
        }
        return page;
    }

    private static void Validate(object request)
    {
        if (request == null)
            throw new NcfExceptionBase("A fine-tuning request is required.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new NcfExceptionBase(string.Join("; ", errors));
    }

    private async Task<T> SendAsync<T>(string workerAlias, HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetAsync(workerAlias, cancellationToken).ConfigureAwait(false);
        using var response = await SendResponseAsync(configuration, method, path, body, cancellationToken).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuration.RequestTimeoutSeconds));
        try
        {
            await response.Content.LoadIntoBufferAsync(MaxResponseBytes, timeout.Token).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, timeout.Token).ConfigureAwait(false)
                ?? throw new NcfExceptionBase("The fine-tuning worker returned an empty response.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid fine-tuning worker JSON for {Path}", path);
            throw new NcfExceptionBase("The fine-tuning worker returned an invalid response.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NcfExceptionBase("The fine-tuning worker response timed out; refresh job status before retrying creation.", ex);
        }
    }

    private async Task<HttpResponseMessage> SendResponseAsync(string workerAlias, HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetAsync(workerAlias, cancellationToken).ConfigureAwait(false);
        return await SendResponseAsync(configuration, method, path, body, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendResponseAsync(FineTuningWorkerConfiguration configuration, HttpMethod method,
        string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(configuration.Endpoint, path));
        request.Headers.Add("X-NCF-Worker-Key", configuration.ApiKey);
        if (body != null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuration.RequestTimeoutSeconds));
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Fine-tuning worker is unreachable for {Method} {Path}: {Error}", method, path, ex.Message);
            throw new NcfExceptionBase(AIKernelResource.Get("FineTuning.Error.Unreachable",
                "Cannot reach the fine-tuning worker. Check its address, authentication and environment."), ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NcfExceptionBase("The fine-tuning worker timed out; refresh job status before retrying creation.", ex);
        }
        if (response.IsSuccessStatusCode)
            return response;
        using (response)
        {
            await response.Content.LoadIntoBufferAsync(MaxResponseBytes, timeout.Token).ConfigureAwait(false);
            var error = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            _logger.LogWarning("Fine-tuning worker rejected {Method} {Path} with HTTP {Status}", method, path, (int)response.StatusCode);
            throw new NcfExceptionBase($"Fine-tuning worker HTTP {(int)response.StatusCode}: {error[..Math.Min(error.Length, 2000)]}");
        }
    }
}
