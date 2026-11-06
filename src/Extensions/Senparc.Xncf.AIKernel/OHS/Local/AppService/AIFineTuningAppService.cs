/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningAppService.cs
    文件功能描述：本地模型微调应用服务


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

    修改标识：Senparc - 20261021
    修改描述：v0.17.0 开放微调导入、导出与产物查询

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance job management and telemetry features

    修改标识：Senparc - 20261106
    修改描述：微调 Function 自动载入 Worker、模型和数据集，并允许 Workflow 按默认值执行

    修改标识：Senparc - 20261106
    修改描述：支持在 NCF 启动后配置并启停多个本机微调 Worker

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
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.AIKernel.OHS.Local.AppService;

[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<AIFineTuningWorkerDto>>> GetWorkersAsync() =>
        this.GetResponseAsync<List<AIFineTuningWorkerDto>>((response, logger) => _configuration.GetWorkersAsync(CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningHealth>> HealthAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningHealth>((response, logger) => _worker.HealthAsync(workerAlias, CancellationToken));

    [FunctionRender("检查微调 Worker", "检查指定微调 Worker 的健康状态、能力和队列情况。只配置了一个可用 Worker 时会自动选中。", typeof(Register), AllowAiInvocation = true)]
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

    [FunctionRender("检查微调环境", "检查微调开关、密钥、Worker、模型目录和 FileManager 状态，不执行安装或命令。只配置了一个可用 Worker 时会自动选中。", typeof(Register), AllowAiInvocation = true)]
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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningModel>>> GetModelsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningModel>>((response, logger) => _worker.GetModelsAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningDataset>>> GetDatasetsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningDataset>>((response, logger) => _worker.GetDatasetsAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningMigrationResult>> MigrateLegacyMetadataAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningMigrationResult>((response, logger) =>
            _migrationService.MigrateAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningCatalogPage<FineTuningDataset>>> GetDatasetsPageAsync(string workerAlias,
        int offset = 0, int limit = 100) =>
        this.GetResponseAsync<FineTuningCatalogPage<FineTuningDataset>>((response, logger) =>
            _worker.GetDatasetsPageAsync(workerAlias, offset, limit, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningDataset>> GetDatasetAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningDataset>((response, logger) => _worker.GetDatasetAsync(workerAlias, id, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    // The dynamic API binds subsequent parameters from query; keep the body DTO first.
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
    [FunctionRender("上传微调数据集文件", "以流方式保存并上传 JSONL 微调数据集，适合大文件。", typeof(Register), AllowAiInvocation = true)]
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

    [FunctionRender("创建微调数据集", "保存经过 Worker 校验的 JSONL 微调数据集。Worker 会自动载入；适合在 Workflow 中提交小规模 JSONL 文本。", typeof(Register), AllowAiInvocation = true)]
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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningJob>>> GetJobsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningJob>>((response, logger) => _worker.GetJobsAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningCatalogPage<FineTuningJob>>> GetJobsPageAsync(string workerAlias,
        int offset = 0, int limit = 100) =>
        this.GetResponseAsync<FineTuningCatalogPage<FineTuningJob>>((response, logger) =>
            _worker.GetJobsPageAsync(workerAlias, offset, limit, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> CreateJobAsync([FromBody] FineTuningJobRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.CreateJobAsync(workerAlias, request, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-created", $"{workerAlias}/{job.Id}");
            return job;
        });

    [FunctionRender("创建微调任务", "使用已批准的本地模型、数据集和训练参数创建微调任务。Worker、模型和数据集会自动载入，训练参数带有默认值，Workflow 可以按这些默认值直接运行。", typeof(Register), AllowAiInvocation = true)]
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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningJob>> GetJobAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.GetJobAsync(workerAlias, id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            return job;
        });

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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningValidation>> GetValidationAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningValidation>((response, logger) => _worker.GetValidationAsync(workerAlias, id, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    [FunctionRender("验证微调 Adapter", "对已成功的微调任务按选择的 CPU、CUDA 或 MLX 后端运行受限的离线测试提示；与任务记录不一致时必须明确确认。", typeof(Register), AllowAiInvocation = true)]
    public Task<AppResponseBase<FineTuningValidation>> StartValidationAsync(
        [FromBody] FineTuningValidationRequest request, string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningValidation>(async (response, logger) =>
        {
            var validation = await _worker.StartValidationAsync(workerAlias, id, request, CancellationToken);
            Audit("job-validation-started", $"{workerAlias}/{id}");
            return validation;
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> CancelJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.CancelJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-cancel-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> PauseJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.PauseJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-pause-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningJob>> ResumeJobAsync([FromBody] FineTuningCancelRequest request, string workerAlias) =>
        this.GetResponseAsync<FineTuningJob>(async (response, logger) =>
        {
            var job = await _worker.ResumeJobAsync(workerAlias, request.Id, CancellationToken);
            await MirrorJobAsync(workerAlias, job);
            Audit("job-resume-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningRuntimeStatus>> GetFineTuningRuntimeAsync() =>
        this.GetResponseAsync<FineTuningRuntimeStatus>((response, logger) => Task.FromResult(_runtime.GetStatus()));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningRuntimeStatus>> SetFineTuningEnabledAsync([FromBody] FineTuningEnabledRequest request) =>
        this.GetResponseAsync<FineTuningRuntimeStatus>((response, logger) =>
        {
            var status = _runtime.SetEnabled(request?.Enabled == true);
            Audit(status.Enabled ? "fine-tuning-enabled" : "fine-tuning-disabled", status.SettingsPath);
            return Task.FromResult(status);
        });

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

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<FineTuningManagedWorkerStatus>> StopManagedWorkerAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningManagedWorkerStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetRequiredAsync(workerAlias, CancellationToken);
            await _supervisor.StopWorkerAsync(worker.Alias, CancellationToken);
            Audit("worker-stopped", worker.Alias);
            return _supervisor.GetStatus(worker);
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningManagedWorkerStatus>> GetManagedWorkerStatusAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningManagedWorkerStatus>(async (response, logger) =>
        {
            var worker = await _workerService.GetRequiredAsync(workerAlias, CancellationToken);
            return _supervisor.GetStatus(worker);
        });

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

    private void Audit(string operation, string id)
    {
        var user = _context.HttpContext?.User;
        _logger.LogInformation("Fine-tuning administration {Operation}: resource={Id}, actor={Actor}, trace={Trace}",
            operation, id, user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name,
            _context.HttpContext?.TraceIdentifier);
    }

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
