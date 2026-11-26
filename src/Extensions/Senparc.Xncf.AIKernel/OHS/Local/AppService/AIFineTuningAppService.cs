/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20261009

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance job management and telemetry features

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261124
    Change description: v0.17.4 Integrated localization, tooling, and runtime improvements across the module.

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.AIKernel.OHS.Local.AppService;

[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
/// <summary>
/// Administrative and function-rendered application-service boundary for fine-tuning workers,
/// datasets, jobs, validation, runtime settings, secrets, and managed worker processes.
/// </summary>
public class AIFineTuningAppService : AppServiceBase
{
    private readonly FineTuningWorkerClient _worker;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger<AIFineTuningAppService> _logger;
    private readonly IFineTuningWorkerConfigurationProvider _configuration;
    private readonly AIFineTuningWorkerService _workerService;
    private readonly FineTuningWorkerOptions _workerOptions;
    private readonly IFineTuningControlPlaneService _controlPlane;
    private readonly FineTuningMigrationService _migrationService;
    private readonly IFineTuningFileStore _fileStore;
    private readonly FineTuningSecretStore _secrets;
    private readonly FineTuningRuntimeSettings _runtime;
    private readonly FineTuningWorkerSupervisor _supervisor;

    /// <summary>
    /// Initializes the fine-tuning application service and its worker/control-plane dependencies.
    /// </summary>
    /// <param name="serviceProvider">Dependency-injection provider used by the base service.</param>
    /// <param name="worker">Remote fine-tuning worker client.</param>
    /// <param name="configuration">Worker configuration provider.</param>
    /// <param name="workerService">Persisted worker metadata service.</param>
    /// <param name="workerOptions">Fine-tuning feature options.</param>
    /// <param name="context">Current HTTP context accessor.</param>
    /// <param name="logger">Application-service logger.</param>
    /// <param name="controlPlane">Local fine-tuning control-plane service.</param>
    /// <param name="migrationService">Legacy metadata migration service.</param>
    /// <param name="fileStore">FileManager-backed dataset store.</param>
    /// <param name="secrets">Worker secret store.</param>
    /// <param name="runtime">Runtime enablement settings.</param>
    /// <param name="supervisor">Managed worker process supervisor.</param>
    public AIFineTuningAppService(IServiceProvider serviceProvider, FineTuningWorkerClient worker,
        IFineTuningWorkerConfigurationProvider configuration, AIFineTuningWorkerService workerService,
        IOptions<FineTuningWorkerOptions> workerOptions, IHttpContextAccessor context,
        ILogger<AIFineTuningAppService> logger, IFineTuningControlPlaneService controlPlane,
        FineTuningMigrationService migrationService, IFineTuningFileStore fileStore,
        FineTuningSecretStore secrets, FineTuningRuntimeSettings runtime, FineTuningWorkerSupervisor supervisor) : base(serviceProvider)
    {
        _worker = worker;
        _configuration = configuration;
        _workerService = workerService;
        _workerOptions = workerOptions.Value;
        _context = context;
        _logger = logger;
        _controlPlane = controlPlane;
        _migrationService = migrationService;
        _fileStore = fileStore;
        _secrets = secrets;
        _runtime = runtime;
        _supervisor = supervisor;
    }

    /// <summary>
    /// Lists database-backed fine-tuning workers visible to the current tenant.
    /// </summary>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<AIFineTuningWorkerDto>>> GetWorkersAsync() =>
        this.GetResponseAsync<List<AIFineTuningWorkerDto>>((response, logger) => _configuration.GetWorkersAsync(CancellationToken));

    /// <summary>
    /// Retrieves health information from a selected fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningHealth>> HealthAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningHealth>((response, logger) => _worker.HealthAsync(workerAlias, CancellationToken));

    [FunctionRender(typeof(AIKernelResource), "Function.FineTuning.CheckWorker.Name", "Function.FineTuning.CheckWorker.Description",
        typeof(Register), "Check fine-tuning Worker", "Check the selected fine-tuning Worker health, capabilities, and queue.", AllowAiInvocation = true)]
    /// <summary>
    /// Function entry point that checks one worker's health and reports configuration failures.
    /// </summary>
    /// <param name="request">The function request containing the worker alias.</param>
    /// <exception cref="ArgumentNullException">Returned through the app-service response when <paramref name="request"/> is <see langword="null"/>.</exception>
    public Task<AppResponseBase<FineTuningHealth>> CheckFineTuningWorkerAsync(FineTuningWorkerFunctionRequest request)
    {
        if (request == null)
        {
            return this.GetResponseAsync<FineTuningHealth>((response, logger) =>
                Task.FromException<FineTuningHealth>(new ArgumentNullException(nameof(request))));
        }

        _logger.LogInformation("Check fine-tuning worker from Function. worker={Worker}", request.WorkerAlias);
        return HealthAsync(request.WorkerAlias);
    }

    /// <summary>
    /// Checks feature enablement, secret configuration, worker reachability, model availability,
    /// and FileManager readiness for a selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningDiagnostics>> DiagnosticsAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningDiagnostics>(async (response, logger) =>
        {
            var diagnostics = new FineTuningDiagnostics
            {
                WorkerAlias = workerAlias,
                FineTuningEnabled = _workerOptions.Enabled,
                SecretConfigured = _secrets.TryRead(workerAlias, out _)
                    || _workerOptions.ConfiguredWorkerAliases.Contains(workerAlias, StringComparer.OrdinalIgnoreCase)
            };
            if (!diagnostics.FineTuningEnabled || !diagnostics.SecretConfigured)
            {
                diagnostics.Status = "ConfigurationRequired";
                return diagnostics;
            }

            try
            {
                var health = await _worker.HealthAsync(workerAlias, CancellationToken);
                var models = await _worker.GetModelsAsync(workerAlias, CancellationToken);
                diagnostics.WorkerReachable = true;
                diagnostics.FileManagerAvailable = await _fileStore.IsAvailableAsync(CancellationToken);
                diagnostics.ModelCount = models.Count;
                diagnostics.WorkerVersion = health.Version;
                diagnostics.Status = diagnostics.FileManagerAvailable ? "Ready" : "FileManagerRequired";
            }
            catch (Exception exception)
            {
                diagnostics.Status = "WorkerUnavailable";
                diagnostics.Error = exception.Message;
            }

            return diagnostics;
        });

    [FunctionRender(typeof(AIKernelResource), "Function.FineTuning.CheckEnvironment.Name", "Function.FineTuning.CheckEnvironment.Description",
        typeof(Register), "Check fine-tuning environment", "Check the fine-tuning switch, secret, Worker, model directory, and FileManager state.", AllowAiInvocation = true)]
    /// <summary>
    /// Function entry point that returns consolidated fine-tuning environment diagnostics.
    /// </summary>
    /// <param name="request">The function request containing the worker alias.</param>
    public Task<AppResponseBase<FineTuningDiagnostics>> CheckFineTuningEnvironmentAsync(FineTuningWorkerFunctionRequest request)
    {
        if (request == null)
        {
            return this.GetResponseAsync<FineTuningDiagnostics>((response, logger) =>
                Task.FromException<FineTuningDiagnostics>(new ArgumentNullException(nameof(request))));
        }

        _logger.LogInformation("Check fine-tuning environment from Function. worker={Worker}", request.WorkerAlias);
        return DiagnosticsAsync(request.WorkerAlias);
    }

    /// <summary>
    /// Lists models exposed by the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningModel>>> GetModelsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningModel>>((response, logger) => _worker.GetModelsAsync(workerAlias, CancellationToken));

    /// <summary>
    /// Lists datasets exposed by the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningDataset>>> GetDatasetsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningDataset>>((response, logger) => _worker.GetDatasetsAsync(workerAlias, CancellationToken));

    /// <summary>
    /// Migrates legacy fine-tuning metadata into the current control-plane records.
    /// </summary>
    /// <param name="workerAlias">The worker alias whose legacy metadata should be migrated.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningMigrationResult>> MigrateLegacyMetadataAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningMigrationResult>((response, logger) =>
            _migrationService.MigrateAsync(workerAlias, CancellationToken));

    /// <summary>
    /// Retrieves a bounded page of worker datasets.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="offset">The zero-based catalog offset.</param>
    /// <param name="limit">The requested page size.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningCatalogPage<FineTuningDataset>>> GetDatasetsPageAsync(string workerAlias,
        int offset = 0, int limit = 100) =>
        this.GetResponseAsync<FineTuningCatalogPage<FineTuningDataset>>((response, logger) =>
            _worker.GetDatasetsPageAsync(workerAlias, offset, limit, CancellationToken));

    /// <summary>
    /// Retrieves one dataset by worker-local identifier.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The dataset identifier.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningDataset>> GetDatasetAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningDataset>((response, logger) => _worker.GetDatasetAsync(workerAlias, id, CancellationToken));

    /// <summary>
    /// Validates and uploads inline JSONL dataset content through FileManager and the worker.
    /// </summary>
    /// <param name="request">The dataset name and JSONL content.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    // The dynamic API binds subsequent parameters from query; keep the body DTO first.
    /// <returns>A response containing the persisted and worker-uploaded dataset.</returns>
    public Task<AppResponseBase<FineTuningDataset>> UploadDatasetAsync([FromBody] FineTuningDatasetRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningDataset>(async (response, logger) =>
        {
            var content = request?.Content ?? string.Empty;
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content), writable: false);
            return await UploadDatasetAndStoreAsync(workerAlias, request?.Name, $"{request?.Name}.jsonl",
                stream, stream.Length, "application/x-ndjson");
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    [RequestSizeLimit(FineTuningWorkerClient.MaxDatasetBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = FineTuningWorkerClient.MaxDatasetBytes + 64 * 1024)]
    [FunctionRender(typeof(AIKernelResource), "Function.FineTuning.UploadDatasetFile.Name", "Function.FineTuning.UploadDatasetFile.Description",
        typeof(Register), "Upload fine-tuning dataset file", "Save and upload a JSONL fine-tuning dataset as a stream for large files.", AllowAiInvocation = true)]
    /// <summary>
    /// Streams a multipart JSONL dataset file through FileManager and the selected worker.
    /// </summary>
    /// <param name="file">The non-empty JSONL upload.</param>
    /// <param name="name">Optional dataset name override.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <exception cref="ArgumentException">Returned through the response when the file is empty, too large, or not JSONL.</exception>
    public Task<AppResponseBase<FineTuningDataset>> UploadDatasetFileAsync(
        [FromForm] IFormFile file, [FromForm] string name, string workerAlias) =>
        this.GetResponseAsync<FineTuningDataset>(async (response, logger) =>
        {
            if (file == null || file.Length <= 0)
            {
                throw new ArgumentException("请选择非空的 JSONL 文件。", nameof(file));
            }

            if (file.Length > FineTuningWorkerClient.MaxDatasetBytes)
            {
                throw new ArgumentException("数据集不能超过 512 MiB。", nameof(file));
            }

            var fileName = Path.GetFileName((file.FileName ?? string.Empty).Replace('\\', '/'));
            if (!fileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("微调数据集必须是 .jsonl 文件。", nameof(file));
            }

            var datasetName = string.IsNullOrWhiteSpace(name)
                ? Path.GetFileNameWithoutExtension(fileName)
                : name.Trim();
            await using var stream = file.OpenReadStream();
            return await UploadDatasetAndStoreAsync(workerAlias, datasetName, fileName, stream, file.Length,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/x-ndjson" : file.ContentType);
        });

    /// <summary>
    /// Imports a dataset into FileManager and creates the corresponding Worker dataset.
    /// </summary>
    /// <param name="workerAlias">The fine-tuning Worker alias.</param>
    /// <param name="name">The dataset display name.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="source">The readable dataset stream.</param>
    /// <param name="length">The declared byte length.</param>
    /// <param name="contentType">The content type passed to FileManager.</param>
    /// <returns>The created Worker dataset.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when dataset name or size is invalid.</exception>
    private async Task<FineTuningDataset> UploadDatasetAndStoreAsync(
        string workerAlias,
        string name,
        string fileName,
        Stream source,
        long length,
        string contentType)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            throw new ArgumentException("数据集名称必须为 1 到 100 个字符。", nameof(name));
        }

        if (length <= 0 || length > FineTuningWorkerClient.MaxDatasetBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "数据集不能超过 512 MiB。");
        }

        if (!await _fileStore.IsAvailableAsync(CancellationToken))
        {
            throw new InvalidOperationException("上传微调数据集需要启用 FileManager。");
        }

        var imported = await _fileStore.ImportAsync(source, fileName, contentType, length,
            $"AIKernel fine-tuning dataset {workerAlias}/{name}", CancellationToken);
        try
        {
            var stored = await _fileStore.OpenReadAsync(imported.FileId, CancellationToken);
            await using (stored.Content)
            {
                var dataset = await _worker.UploadDatasetFileAsync(workerAlias, name, stored.Content,
                    stored.Length, CancellationToken);
                await _controlPlane.UpsertDatasetAsync(new AIFineTuningDataset(workerAlias, dataset.Id, dataset.Name,
                    dataset.Rows, dataset.Sha256, dataset.CreatedUtc.UtcDateTime, imported.FileId), CancellationToken);
                Audit("dataset-uploaded", $"{workerAlias}/{dataset.Id}");
                return dataset;
            }
        }
        catch (Exception exception)
        {
            try
            {
                await _fileStore.DeleteAsync(imported.FileId, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up FileManager dataset {FileId} after upload failure.",
                    imported.FileId);
            }

            _logger.LogError(exception, "Fine-tuning dataset upload failed after FileManager import {FileId}.",
                imported.FileId);
            throw;
        }
    }

    [FunctionRender(typeof(AIKernelResource), "Function.FineTuning.CreateDataset.Name", "Function.FineTuning.CreateDataset.Description",
        typeof(Register), "Create fine-tuning dataset", "Save a Worker-validated JSONL fine-tuning dataset.", AllowAiInvocation = true)]
    /// <summary>
    /// Function entry point that stores and uploads JSONL dataset text.
    /// </summary>
    /// <param name="request">The worker, dataset name, and JSONL content.</param>
    public Task<AppResponseBase<FineTuningDataset>> CreateDatasetFromTextAsync(FineTuningDatasetFunctionRequest request) =>
        this.GetResponseAsync<FineTuningDataset>(async (response, logger) =>
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.WorkerAlias))
            {
                throw new InvalidOperationException("请选择微调 Worker。当前没有可自动载入的已启用 Worker。");
            }

            _logger.LogInformation(
                "Create fine-tuning dataset from Function. worker={Worker} name={Name} contentLength={Length}",
                request.WorkerAlias, request.Name, request.Content?.Length ?? 0);
            var content = request.Content ?? string.Empty;
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content), writable: false);
            return await UploadDatasetAndStoreAsync(request.WorkerAlias, request.Name, $"{request.Name}.jsonl",
                stream, stream.Length, "application/x-ndjson");
        });

    /// <summary>
    /// Lists jobs exposed by the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningJob>>> GetJobsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningJob>>((response, logger) => _worker.GetJobsAsync(workerAlias, CancellationToken));

    /// <summary>
    /// Retrieves a bounded page of jobs from the selected worker.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="offset">The zero-based catalog offset.</param>
    /// <param name="limit">The requested page size.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningCatalogPage<FineTuningJob>>> GetJobsPageAsync(string workerAlias,
        int offset = 0, int limit = 100) =>
        this.GetResponseAsync<FineTuningCatalogPage<FineTuningJob>>((response, logger) =>
            _worker.GetJobsPageAsync(workerAlias, offset, limit, CancellationToken));

    /// <summary>
    /// Validates, creates, mirrors, and returns a fine-tuning job.
    /// </summary>
    /// <param name="request">The model, dataset, backend, and training parameters.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <exception cref="NcfExceptionBase">Returned through the response when validation or worker creation fails.</exception>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> CreateJobAsync([FromBody] FineTuningJobRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.CreateJobAsync(workerAlias, request, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-created", $"{workerAlias}/{job.Id}");
            return job;
        });

    [FunctionRender(
        typeof(AIKernelResource),
        "Frontend.AIKernel.CreateFineTuningTask",
        "Frontend.AIKernel.CreateAFineTuningTaskWithAnApprovedLocalModelDatasetAnd",
        typeof(Register),
        "Create fine-tuning task",
        "Create a fine-tuning task with an approved local model, dataset, and training parameters.",
        AllowAiInvocation = true)]
    /// <summary>
    /// Function entry point that creates a fine-tuning job from approved model/dataset selections.
    /// </summary>
    /// <param name="request">The function-level fine-tuning request.</param>
    public Task<AppResponseBase<FineTuningJob>> StartFineTuningJobAsync(FineTuningStartJobFunctionRequest request) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var workerAlias = request.ResolveWorkerAlias();
            var jobRequest = request.ToJobRequest();
            _logger.LogInformation(
                "Start fine-tuning from Function. worker={Worker} name={Name} model={Model} dataset={Dataset} eval={Eval} backend={Backend} method={Method} epochs={Epochs} maxSteps={MaxSteps} learningRate={LearningRate} batchSize={BatchSize} maxDurationMinutes={MaxDuration}",
                workerAlias,
                jobRequest.Name,
                jobRequest.ModelId,
                jobRequest.DatasetId,
                jobRequest.EvalDatasetId,
                jobRequest.Backend,
                jobRequest.Method,
                jobRequest.Epochs,
                jobRequest.MaxSteps,
                jobRequest.LearningRate,
                jobRequest.BatchSize,
                jobRequest.MaxDurationMinutes);
            var job = await _worker.CreateJobAsync(workerAlias!, jobRequest, CancellationToken);
            await MirrorJobAsync(workerAlias!, job);
            Audit("job-created", $"{workerAlias}/{job.Id}");
            return job;
        });

    /// <summary>
    /// Retrieves and mirrors one fine-tuning job.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The job identifier.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningJob>> GetJobAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.GetJobAsync(workerAlias, id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            return job;
        });

    /// <summary>
    /// Retrieves a bounded event page and mirrors returned events to the local control plane.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The job identifier.</param>
    /// <param name="after">The event sequence cursor.</param>
    /// <param name="limit">The requested event count, bounded by the worker contract.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningEventPage>> GetEventsAsync(string workerAlias, string id, long after = 0, int limit = 200) =>
        this.GetResponseAsync<FineTuningEventPage>(async (response, logger) =>
        {
            var page = await _worker.GetEventsAsync(workerAlias, id, after, limit, CancellationToken);
            foreach (var item in page.Events)
            {
                await _controlPlane.UpsertEventAsync(new AIFineTuningEvent(workerAlias, id, item.Sequence,
                    item.TimestampUtc.UtcDateTime, item.Kind, item.Message ?? string.Empty,
                    JsonSerializer.Serialize(item.Metrics)), CancellationToken);
            }

            return page;
        });

    /// <summary>
    /// Retrieves the latest validation status for a fine-tuning job.
    /// </summary>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The job identifier.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningValidation>> GetValidationAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningValidation>((response, logger) => _worker.GetValidationAsync(workerAlias, id, CancellationToken));

    /// <summary>
    /// Starts bounded validation for a successful fine-tuning job.
    /// </summary>
    /// <param name="request">Validation prompt and execution options.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The job identifier.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    [FunctionRender(typeof(AIKernelResource), "Function.FineTuning.ValidateAdapter.Name", "Function.FineTuning.ValidateAdapter.Description",
        typeof(Register), "Validate fine-tuning adapter", "Run a bounded offline test prompt for a successful fine-tuning job on the selected backend.", AllowAiInvocation = true)]
    /// <returns>A response containing the validation result.</returns>
    public Task<AppResponseBase<FineTuningValidation>> StartValidationAsync(
        [FromBody] FineTuningValidationRequest request, string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningValidation>(async (response, logger) =>
        {
            var validation = await _worker.StartValidationAsync(workerAlias, id, request, CancellationToken);
            Audit("job-validation-started", $"{workerAlias}/{id}");
            return validation;
        });

    /// <summary>
    /// Requests cancellation of a fine-tuning job and mirrors the resulting state.
    /// </summary>
    /// <param name="request">The request containing the job identifier.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> CancelJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.CancelJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-cancel-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    /// <summary>
    /// Requests pausing of a fine-tuning job and mirrors the resulting state.
    /// </summary>
    /// <param name="request">The request containing the job identifier.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> PauseJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.PauseJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-pause-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    /// <summary>
    /// Requests resumption of a paused fine-tuning job and mirrors the resulting state.
    /// </summary>
    /// <param name="request">The request containing the job identifier.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> ResumeJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.ResumeJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-resume-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    /// <summary>
    /// Updates runtime parameters for an existing fine-tuning job.
    /// </summary>
    /// <param name="request">The runtime parameters to apply.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    /// <param name="id">The job identifier.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> UpdateRuntimeAsync(
        [FromBody] FineTuningRuntimeRequest request, string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.UpdateRuntimeAsync(workerAlias, id, request, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-runtime-updated", $"{workerAlias}/{job.Id}");
            return job;
        });

    /// <summary>
    /// Deletes a fine-tuning job and removes mirrored local metadata/files.
    /// </summary>
    /// <param name="request">The request containing the job identifier.</param>
    /// <param name="workerAlias">The configured worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningDeleteResult>> DeleteJobAsync(
        [FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningDeleteResult>(async (response, logger) =>
        {
            var deleted = await _worker.DeleteJobAsync(workerAlias, request.Id, CancellationToken);
            var fileIds = await _controlPlane.DeleteJobAsync(workerAlias, request.Id, CancellationToken);
            foreach (var fileId in fileIds)
            {
                await _fileStore.DeleteAsync(fileId, CancellationToken);
            }

            Audit("job-deleted", $"{workerAlias}/{deleted.Id}");
            return deleted;
        });

    /// <summary>
    /// Returns current local fine-tuning runtime settings and enablement state.
    /// </summary>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningRuntimeStatus>> GetFineTuningRuntimeAsync() =>
        this.GetResponseAsync<FineTuningRuntimeStatus>((response, logger) => Task.FromResult(_runtime.GetStatus()));

    /// <summary>
    /// Updates local fine-tuning enablement after applying runtime settings.
    /// </summary>
    /// <param name="request">The enablement request.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningRuntimeStatus>> SetFineTuningEnabledAsync([FromBody] FineTuningEnabledRequest request) =>
        this.GetResponseAsync<FineTuningRuntimeStatus>((response, logger) =>
        {
            var status = _runtime.SetEnabled(request?.Enabled == true);
            Audit(status.Enabled ? "fine-tuning-enabled" : "fine-tuning-disabled", status.SettingsPath);
            return Task.FromResult(status);
        });

    /// <summary>
    /// Generates or rotates the local secret used by a fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The worker alias whose secret should be generated.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningSecretStatus>> GenerateWorkerSecretAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningSecretStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetRequiredAsync(workerAlias, CancellationToken);
            var generated = _secrets.Generate(worker.Alias);
            _logger.LogInformation("Fine-tuning worker secret {Action} for {Alias} at {Path}.",
                generated.Created ? "created" : "already present", worker.Alias, generated.Path);
            Audit(generated.Created ? "worker-secret-created" : "worker-secret-present", worker.Alias);
            return new FineTuningSecretStatus
            {
                Alias = worker.Alias,
                Configured = true,
                Path = generated.Path,
                Created = generated.Created
            };
        });

    /// <summary>
    /// Starts a configured managed fine-tuning worker process.
    /// </summary>
    /// <param name="workerAlias">The worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningManagedWorkerStatus>> StartManagedWorkerAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningManagedWorkerStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetEnabledAsync(workerAlias, CancellationToken);
            var peers = await _workerService.GetAllAsync(CancellationToken);
            _logger.LogInformation("Start managed fine-tuning worker {Alias} at {Endpoint}.", worker.Alias, worker.Endpoint);
            var status = await _supervisor.StartAsync(worker, peers, CancellationToken);
            Audit("worker-started", worker.Alias);
            return status;
        });

    /// <summary>
    /// Stops a managed fine-tuning worker process.
    /// </summary>
    /// <param name="workerAlias">The worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningManagedWorkerStatus>> StopManagedWorkerAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningManagedWorkerStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetRequiredAsync(workerAlias, CancellationToken);
            await _supervisor.StopWorkerAsync(worker.Alias, CancellationToken);
            Audit("worker-stopped", worker.Alias);
            return _supervisor.GetStatus(worker);
        });

    /// <summary>
    /// Returns the current status of a managed fine-tuning worker.
    /// </summary>
    /// <param name="workerAlias">The worker alias.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningManagedWorkerStatus>> GetManagedWorkerStatusAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningManagedWorkerStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetRequiredAsync(workerAlias, CancellationToken);
            return _supervisor.GetStatus(worker);
        });

    /// <summary>
    /// Creates or updates persisted worker configuration and returns the saved DTO.
    /// </summary>
    /// <param name="request">The worker identity, endpoint, secret, and managed-launch settings.</param>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<AIFineTuningWorkerDto>> SaveWorkerAsync([FromBody] AIFineTuningWorker_CreateOrEditRequest request) =>
        this.GetResponseAsync<AIFineTuningWorkerDto>(async (response, logger) =>
        {
            var aliases = _workerOptions.ConfiguredWorkerAliases.Concat(_secrets.ListConfiguredAliases());
            var worker = await _workerService.SaveAsync(request, aliases, CancellationToken);
            if (worker.Enabled)
                _runtime.SetEnabled(true);
            if (Uri.TryCreate(worker.Endpoint, UriKind.Absolute, out var endpoint))
                _runtime.AllowHost(endpoint.Host);
            try
            {
                worker.SecretPath = _secrets.PathFor(worker.Alias);
            }
            catch (NcfExceptionBase)
            {
                worker.SecretPath = "";
            }

            Audit("worker-saved", worker.Alias);
            return worker;
        });

    /// <summary>
    /// Writes an audit log entry containing the current actor and trace identifier.
    /// </summary>
    /// <param name="operation">The administrative operation name.</param>
    /// <param name="id">The affected resource identifier.</param>
    private void Audit(string operation, string id)
    {
        var user = _context.HttpContext?.User;
        _logger.LogInformation("Fine-tuning administration {Operation}: resource={Id}, actor={Actor}, trace={Trace}",
            operation, id, user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name,
            _context.HttpContext?.TraceIdentifier);
    }

    /// <summary>
    /// Mirrors a Worker job and its artifacts into the local control-plane store.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="job">The Worker job snapshot.</param>
    /// <returns>A task that completes after job and artifact persistence.</returns>
    private async Task MirrorJobAsync(string workerAlias, FineTuningJob job)
    {
        await _controlPlane.UpsertJobAsync(new AIFineTuningJob(workerAlias, job.Id, job.Name, job.State,
            job.CreatedUtc.UtcDateTime, job.StartedUtc?.UtcDateTime, job.FinishedUtc?.UtcDateTime,
            JsonSerializer.Serialize(job.Request), job.Error ?? string.Empty, JsonSerializer.Serialize(job.LatestMetrics),
            job.LastEventSequence), CancellationToken);

        foreach (var artifact in job.Artifacts)
        {
            await _controlPlane.UpsertArtifactAsync(new AIFineTuningArtifact(workerAlias, job.Id, artifact.Id,
                artifact.Name, artifact.Bytes), CancellationToken);
        }

    }
}
