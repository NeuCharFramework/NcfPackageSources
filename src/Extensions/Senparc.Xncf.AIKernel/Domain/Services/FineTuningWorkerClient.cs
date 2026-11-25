/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningWorkerClient.cs
    File description: Related implementation.

    Created by: Senparc - 20261009

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance job management and telemetry features

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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
    public const long MaxDatasetBytes = 512L * 1024 * 1024;
    public const int MaxDatasetRows = 1_000_000;
    private const long MaxResponseBytes = 8 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex SafeIdentifier = new("^[a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}$", RegexOptions.CultureInvariant);
    private readonly HttpClient _http;
    private readonly IFineTuningWorkerConfigurationProvider _configuration;
    private readonly ILogger<FineTuningWorkerClient> _logger;

    /// <summary>
    /// Initializes a client for a configured fine-tuning worker endpoint.
    /// </summary>
    /// <param name="http">HTTP client used to communicate with the worker.</param>
    /// <param name="configuration">Provider used to resolve a worker alias into endpoint and credential settings.</param>
    /// <param name="logger">Logger used to record transport and response validation failures.</param>
    [ActivatorUtilitiesConstructor]
    public FineTuningWorkerClient(HttpClient http, IFineTuningWorkerConfigurationProvider configuration,
        ILogger<FineTuningWorkerClient> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the legacy client using the options-based default worker configuration.
    /// </summary>
    /// <param name="http">HTTP client used to communicate with the worker.</param>
    /// <param name="options">Legacy worker endpoint and credential options.</param>
    /// <param name="logger">Logger used to record transport and response validation failures.</param>
    [Obsolete("Use the database-backed worker configuration provider.")]
    public FineTuningWorkerClient(HttpClient http, IOptions<FineTuningWorkerOptions> options,
        ILogger<FineTuningWorkerClient> logger)
        : this(http, new LegacyWorkerConfigurationProvider(options.Value), logger)
    {
    }

    /// <summary>
    /// Retrieves the health document from the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker health payload.</returns>
    public Task<FineTuningHealth> HealthAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningHealth>(workerAlias, HttpMethod.Get, "health", null, cancellationToken);

    /// <summary>
    /// Retrieves health from the legacy default worker configuration.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker health payload.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningHealth> HealthAsync(CancellationToken cancellationToken = default) => HealthAsync("default", cancellationToken);

    /// <summary>
    /// Lists all models exposed by the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's model list.</returns>
    public Task<List<FineTuningModel>> GetModelsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningModel>>(workerAlias, HttpMethod.Get, "models", null, cancellationToken);

    /// <summary>
    /// Lists models from the legacy default worker configuration.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's model list.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningModel>> GetModelsAsync(CancellationToken cancellationToken = default) => GetModelsAsync("default", cancellationToken);

    /// <summary>
    /// Lists all datasets exposed by the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's dataset list.</returns>
    public Task<List<FineTuningDataset>> GetDatasetsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningDataset>>(workerAlias, HttpMethod.Get, "datasets", null, cancellationToken);

    /// <summary>
    /// Retrieves one page of datasets from the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="offset">The zero-based offset into the worker catalog.</param>
    /// <param name="limit">The maximum number of items requested; the worker contract limits this to 200.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the validated dataset page.</returns>
    public Task<FineTuningCatalogPage<FineTuningDataset>> GetDatasetsPageAsync(string workerAlias, int offset = 0,
        int limit = 100, CancellationToken cancellationToken = default) =>
        GetCatalogAsync<FineTuningDataset>(workerAlias, "datasets", offset, limit, cancellationToken);

    /// <summary>
    /// Retrieves one dataset by its worker-local identifier.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The dataset identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the dataset metadata.</returns>
    public Task<FineTuningDataset> GetDatasetAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningDataset>(workerAlias, HttpMethod.Get, $"datasets/{Identifier(id)}", null, cancellationToken);

    /// <summary>
    /// Lists datasets from the legacy default worker configuration.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's dataset list.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningDataset>> GetDatasetsAsync(CancellationToken cancellationToken = default) => GetDatasetsAsync("default", cancellationToken);

    /// <summary>
    /// Validates and uploads an in-memory NDJSON dataset to the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="request">The dataset name and UTF-8 NDJSON content.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    /// <returns>A task that completes with the created dataset metadata.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the request is invalid or exceeds the 512 MiB dataset limit.</exception>
    public async Task<FineTuningDataset> UploadDatasetAsync(string workerAlias, FineTuningDatasetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        if (Encoding.UTF8.GetByteCount(request.Content) > MaxDatasetBytes)
            throw new NcfExceptionBase("Dataset exceeds the 512 MiB UTF-8 limit.");
        var content = Encoding.UTF8.GetBytes(request.Content);
        await using var stream = new MemoryStream(content, writable: false);
        return await UploadDatasetFileAsync(workerAlias, request.Name, stream, content.LongLength, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and uploads dataset content from a readable stream.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="name">The dataset name sent to the worker.</param>
    /// <param name="content">A readable stream containing the dataset payload.</param>
    /// <param name="length">The byte length of <paramref name="content"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    /// <returns>A task that completes with the created dataset metadata.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the name, stream, or length violates the client limits.</exception>
    public Task<FineTuningDataset> UploadDatasetFileAsync(string workerAlias, string name, Stream content, long length,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new NcfExceptionBase("Dataset name must be between 1 and 100 characters.");
        if (content == null || !content.CanRead)
            throw new NcfExceptionBase("Dataset content is required.");
        if (length < 1 || length > MaxDatasetBytes)
            throw new NcfExceptionBase("Dataset exceeds the 512 MiB UTF-8 limit.");
        return SendStreamAsync<FineTuningDataset>(workerAlias, HttpMethod.Post,
            $"datasets/file?name={Uri.EscapeDataString(name)}", content, length, cancellationToken);
    }

    /// <summary>
    /// Uploads a dataset using the legacy default worker configuration.
    /// </summary>
    /// <param name="request">The dataset name and UTF-8 NDJSON content.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    /// <returns>A task that completes with the created dataset metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningDataset> UploadDatasetAsync(FineTuningDatasetRequest request, CancellationToken cancellationToken = default) =>
        UploadDatasetAsync("default", request, cancellationToken);

    /// <summary>
    /// Lists all jobs exposed by the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's job list.</returns>
    public Task<List<FineTuningJob>> GetJobsAsync(string workerAlias, CancellationToken cancellationToken = default) =>
        SendAsync<List<FineTuningJob>>(workerAlias, HttpMethod.Get, "jobs", null, cancellationToken);

    /// <summary>
    /// Retrieves one page of jobs from the selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="offset">The zero-based offset into the worker catalog.</param>
    /// <param name="limit">The maximum number of jobs requested.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the validated job page.</returns>
    public Task<FineTuningCatalogPage<FineTuningJob>> GetJobsPageAsync(string workerAlias, int offset = 0,
        int limit = 100, CancellationToken cancellationToken = default) =>
        GetCatalogAsync<FineTuningJob>(workerAlias, "jobs", offset, limit, cancellationToken);
    /// <summary>
    /// Lists jobs from the legacy default worker configuration.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's job list.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<List<FineTuningJob>> GetJobsAsync(CancellationToken cancellationToken = default) => GetJobsAsync("default", cancellationToken);

    /// <summary>
    /// Validates a fine-tuning job request and submits it to the selected worker.
    /// Dataset and model identifiers are checked locally, and training and evaluation
    /// datasets must not be the same.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="request">The model, dataset, backend, and fine-tuning options.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the created job metadata.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when validation fails, identifiers are invalid, datasets are equal, or QLoRA is requested on the CPU backend.</exception>
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
    /// <summary>
    /// Creates a job using the legacy default worker configuration.
    /// </summary>
    /// <param name="request">The model, dataset, backend, and fine-tuning options.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the created job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> CreateJobAsync(FineTuningJobRequest request, CancellationToken cancellationToken = default) =>
        CreateJobAsync("default", request, cancellationToken);

    /// <summary>
    /// Retrieves one job by its worker-local identifier.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the job metadata.</returns>
    public Task<FineTuningJob> GetJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}", null, cancellationToken);

    /// <summary>
    /// Retrieves a job from the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> GetJobAsync(string id, CancellationToken cancellationToken = default) => GetJobAsync("default", id, cancellationToken);

    /// <summary>
    /// Requests cancellation of one job on the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    public Task<FineTuningJob> CancelJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/cancel", new { }, cancellationToken);

    /// <summary>
    /// Requests cancellation using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> CancelJobAsync(string id, CancellationToken cancellationToken = default) => CancelJobAsync("default", id, cancellationToken);

    /// <summary>
    /// Requests that one job pause on the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    public Task<FineTuningJob> PauseJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/pause", new { }, cancellationToken);

    /// <summary>
    /// Requests that one job pause using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> PauseJobAsync(string id, CancellationToken cancellationToken = default) => PauseJobAsync("default", id, cancellationToken);

    /// <summary>
    /// Requests that one job resume on the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    public Task<FineTuningJob> ResumeJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/resume", new { }, cancellationToken);

    /// <summary>
    /// Requests that one job resume using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> ResumeJobAsync(string id, CancellationToken cancellationToken = default) => ResumeJobAsync("default", id, cancellationToken);

    /// <summary>
    /// Updates runtime settings for an existing job after validating the request.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="request">The runtime settings to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the request is invalid or the job identifier is not accepted.</exception>
    public Task<FineTuningJob> UpdateRuntimeAsync(string workerAlias, string id, FineTuningRuntimeRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        return SendAsync<FineTuningJob>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/runtime", request, cancellationToken);
    }
    /// <summary>
    /// Updates runtime settings using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="request">The runtime settings to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the updated job metadata.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningJob> UpdateRuntimeAsync(string id, FineTuningRuntimeRequest request,
        CancellationToken cancellationToken = default) => UpdateRuntimeAsync("default", id, request, cancellationToken);

    /// <summary>
    /// Permanently deletes a job from the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's deletion result.</returns>
    public Task<FineTuningDeleteResult> DeleteJobAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningDeleteResult>(workerAlias, HttpMethod.Delete, $"jobs/{Identifier(id)}", null, cancellationToken);

    /// <summary>
    /// Deletes a job using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker's deletion result.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningDeleteResult> DeleteJobAsync(string id, CancellationToken cancellationToken = default) =>
        DeleteJobAsync("default", id, cancellationToken);

    /// <summary>
    /// Retrieves a bounded page of job events after the supplied event cursor.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="after">The non-negative event sequence after which results are requested.</param>
    /// <param name="limit">The number of events requested, from 1 through 200.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the event page.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the cursor or page size is outside the supported range.</exception>
    public Task<FineTuningEventPage> GetEventsAsync(string workerAlias, string id, long after, int limit, CancellationToken cancellationToken = default)
    {
        if (after < 0 || limit < 1 || limit > 200)
            throw new NcfExceptionBase("Event cursor must be nonnegative and limit must be between 1 and 200.");
        return SendAsync<FineTuningEventPage>(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}/events?after={after}&limit={limit}", null, cancellationToken);
    }
    /// <summary>
    /// Retrieves job events using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="after">The non-negative event sequence after which results are requested.</param>
    /// <param name="limit">The number of events requested, from 1 through 200.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the event page.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<FineTuningEventPage> GetEventsAsync(string id, long after, int limit, CancellationToken cancellationToken = default) =>
        GetEventsAsync("default", id, after, limit, cancellationToken);

    /// <summary>
    /// Retrieves the validation status for a job on the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the validation status.</returns>
    public Task<FineTuningValidation> GetValidationAsync(string workerAlias, string id, CancellationToken cancellationToken = default) =>
        SendAsync<FineTuningValidation>(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}/validation", null, cancellationToken);

    /// <summary>
    /// Starts validation for a job using the supplied validation options.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="request">The validation options to submit.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the validation status.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the validation request is invalid or the job identifier is not accepted.</exception>
    public Task<FineTuningValidation> StartValidationAsync(string workerAlias, string id,
        FineTuningValidationRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        return SendAsync<FineTuningValidation>(workerAlias, HttpMethod.Post, $"jobs/{Identifier(id)}/validation", request, cancellationToken);
    }

    /// <summary>
    /// Opens a streaming response for a job artifact without buffering the archive in web memory.
    /// The caller owns the returned response and must dispose it after consuming the stream.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="artifactId">The artifact identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the open HTTP response.</returns>
    public Task<HttpResponseMessage> DownloadArtifactAsync(string workerAlias, string id, string artifactId, CancellationToken cancellationToken = default) =>
        SendResponseAsync(workerAlias, HttpMethod.Get, $"jobs/{Identifier(id)}/artifacts/{Identifier(artifactId)}", null, cancellationToken);

    /// <summary>
    /// Opens a job artifact using the legacy default worker configuration.
    /// </summary>
    /// <param name="id">The job identifier accepted by the worker identifier policy.</param>
    /// <param name="artifactId">The artifact identifier accepted by the worker identifier policy.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the open HTTP response.</returns>
    [Obsolete("Select a database worker explicitly.")]
    public Task<HttpResponseMessage> DownloadArtifactAsync(string id, string artifactId, CancellationToken cancellationToken = default) =>
        DownloadArtifactAsync("default", id, artifactId, cancellationToken);

    private sealed class LegacyWorkerConfigurationProvider : IFineTuningWorkerConfigurationProvider
    {
        private readonly FineTuningWorkerConfiguration _configuration;

        /// <summary>
        /// Adapts legacy options into the configuration provider contract used by the client.
        /// </summary>
        /// <param name="options">The legacy worker options to convert.</param>
        public LegacyWorkerConfigurationProvider(FineTuningWorkerOptions options) =>
            _configuration = options.GetLegacyConfiguration();

        /// <summary>
        /// Returns the legacy configuration only for the reserved <c>default</c> alias.
        /// </summary>
        /// <param name="workerAlias">The worker alias requested by the client.</param>
        /// <param name="cancellationToken">Unused cancellation token retained for provider compatibility.</param>
        /// <returns>A task that completes with the legacy worker configuration.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when an alias other than <c>default</c> is requested.</exception>
        public Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default) =>
            workerAlias == "default" ? Task.FromResult(_configuration)
                : throw new NcfExceptionBase("The legacy fine-tuning client only supports the default worker.");

        /// <summary>
        /// Returns no database-backed workers because the legacy provider exposes one implicit worker.
        /// </summary>
        /// <param name="cancellationToken">Unused cancellation token retained for provider compatibility.</param>
        /// <returns>An empty worker list.</returns>
        public Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AIFineTuningWorkerDto>());
    }

    /// <summary>
    /// Validates a worker-local resource identifier and returns it unchanged.
    /// Identifiers must start with an ASCII letter or digit, may contain letters,
    /// digits, periods, underscores, and hyphens, and may not contain parent traversal.
    /// </summary>
    /// <param name="value">The identifier to validate.</param>
    /// <returns>The original identifier when it satisfies the worker identifier policy.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the identifier is null, empty, malformed, or contains parent traversal.</exception>
    public static string Identifier(string value)
    {
        if (value == null || !SafeIdentifier.IsMatch(value) || value.Contains("..", StringComparison.Ordinal))
            throw new NcfExceptionBase("Invalid fine-tuning resource identifier.");
        return value;
    }

    /// <summary>
    /// Builds a bounded catalog page path after validating the requested range.
    /// </summary>
    /// <param name="catalog">The worker catalog route, such as <c>jobs</c> or <c>datasets</c>.</param>
    /// <param name="offset">The zero-based catalog offset.</param>
    /// <param name="limit">The number of items requested.</param>
    /// <returns>The worker-relative catalog page path.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the offset or limit is outside the supported range.</exception>
    private static string CatalogPath(string catalog, int offset, int limit)
    {
        if (offset < 0 || offset > 100000 || limit < 1 || limit > 200)
            throw new NcfExceptionBase("Catalog offset must be between 0 and 100000 and limit between 1 and 200.");
        return $"{catalog}/page?offset={offset}&limit={limit}";
    }

    /// <summary>
    /// Retrieves and validates one typed catalog page from a worker.
    /// </summary>
    /// <typeparam name="T">The item type expected in the catalog page.</typeparam>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="catalog">The worker catalog route.</param>
    /// <param name="offset">The requested zero-based offset.</param>
    /// <param name="limit">The requested page size.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the validated page.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the worker returns an inconsistent page shape.</exception>
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

    /// <summary>
    /// Applies data-annotation validation to a fine-tuning request object.
    /// </summary>
    /// <param name="request">The request object to validate.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the request is null or contains validation errors.</exception>
    private static void Validate(object request)
    {
        if (request == null)
            throw new NcfExceptionBase("A fine-tuning request is required.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new NcfExceptionBase(string.Join("; ", errors));
    }

    /// <summary>
    /// Sends a JSON request to a configured worker and deserializes a bounded response body.
    /// </summary>
    /// <typeparam name="T">The response type expected from the worker.</typeparam>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="path">The worker-relative request path.</param>
    /// <param name="body">The optional JSON request body.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the deserialized worker response.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the worker response is empty, invalid JSON, or times out.</exception>
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

    /// <summary>
    /// Sends a streamed NDJSON request to a configured worker and deserializes its bounded response body.
    /// </summary>
    /// <typeparam name="T">The response type expected from the worker.</typeparam>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="path">The worker-relative request path.</param>
    /// <param name="content">The readable request stream.</param>
    /// <param name="length">The byte length sent in the request headers.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the deserialized worker response.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the worker response is empty, invalid JSON, or times out.</exception>
    private async Task<T> SendStreamAsync<T>(string workerAlias, HttpMethod method, string path,
        Stream content, long length, CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetAsync(workerAlias, cancellationToken).ConfigureAwait(false);
        using var requestContent = new StreamContent(content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");
        requestContent.Headers.ContentLength = length;
        using var response = await SendResponseAsync(configuration, method, path, requestContent, cancellationToken)
            .ConfigureAwait(false);
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

    /// <summary>
    /// Resolves a worker alias and sends a request with an optional JSON body.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias to contact.</param>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="path">The worker-relative request path.</param>
    /// <param name="body">The optional object serialized as JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker response headers and body stream.</returns>
    private async Task<HttpResponseMessage> SendResponseAsync(string workerAlias, HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var configuration = await _configuration.GetAsync(workerAlias, cancellationToken).ConfigureAwait(false);
        return await SendResponseAsync(configuration, method, path, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts an optional object body to JSON content and delegates the request to the
    /// content-based transport overload.
    /// </summary>
    /// <param name="configuration">The resolved worker endpoint and authentication settings.</param>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="path">The worker-relative request path.</param>
    /// <param name="body">The optional object serialized as JSON.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with the worker response.</returns>
    private async Task<HttpResponseMessage> SendResponseAsync(FineTuningWorkerConfiguration configuration, HttpMethod method,
        string path, object? body, CancellationToken cancellationToken)
    {
        HttpContent? content = body == null ? null : JsonContent.Create(body, options: JsonOptions);
        return await SendResponseAsync(configuration, method, path, content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request to a fine-tuning worker, applying the worker API key,
    /// request timeout, response streaming mode, and non-success response conversion.
    /// </summary>
    /// <param name="configuration">The resolved worker endpoint, API key, and timeout settings.</param>
    /// <param name="method">The HTTP method used for the request.</param>
    /// <param name="path">The worker-relative request path.</param>
    /// <param name="content">The optional HTTP content body.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A task that completes with a successful worker response.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when transport fails, the request times out, or the worker returns a non-success status.</exception>
    private async Task<HttpResponseMessage> SendResponseAsync(FineTuningWorkerConfiguration configuration, HttpMethod method,
        string path, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(configuration.Endpoint, path));
        request.Headers.Add("X-NCF-Worker-Key", configuration.ApiKey);
        request.Content = content;
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
