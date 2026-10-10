/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningAppService.cs
    文件功能描述：本地模型微调应用服务


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
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

    public AIFineTuningAppService(IServiceProvider serviceProvider, FineTuningWorkerClient worker,
        IFineTuningWorkerConfigurationProvider configuration, AIFineTuningWorkerService workerService,
        IOptions<FineTuningWorkerOptions> workerOptions, IHttpContextAccessor context,
        ILogger<AIFineTuningAppService> logger) : base(serviceProvider)
    {
        _worker = worker;
        _configuration = configuration;
        _workerService = workerService;
        _workerOptions = workerOptions.Value;
        _context = context;
        _logger = logger;
    }

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<AIFineTuningWorkerDto>>> GetWorkersAsync() =>
        this.GetResponseAsync<List<AIFineTuningWorkerDto>>((response, logger) => _configuration.GetWorkersAsync(CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningHealth>> HealthAsync(string workerAlias) =>
        this.GetResponseAsync<FineTuningHealth>((response, logger) => _worker.HealthAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningModel>>> GetModelsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningModel>>((response, logger) => _worker.GetModelsAsync(workerAlias, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<List<FineTuningDataset>>> GetDatasetsAsync(string workerAlias) =>
        this.GetResponseAsync<List<FineTuningDataset>>((response, logger) => _worker.GetDatasetsAsync(workerAlias, CancellationToken));

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
            var dataset = await _worker.UploadDatasetAsync(workerAlias, request, CancellationToken);
            Audit("dataset-uploaded", $"{workerAlias}/{dataset.Id}");
            return dataset;
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
            Audit("job-created", $"{workerAlias}/{job.Id}");
            return job;
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningJob>> GetJobAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningJob>((response, logger) => _worker.GetJobAsync(workerAlias, id, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningEventPage>> GetEventsAsync(string workerAlias, string id, long after = 0, int limit = 200) =>
        this.GetResponseAsync<FineTuningEventPage>((response, logger) => _worker.GetEventsAsync(workerAlias, id, after, limit, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public Task<AppResponseBase<FineTuningValidation>> GetValidationAsync(string workerAlias, string id) =>
        this.GetResponseAsync<FineTuningValidation>((response, logger) => _worker.GetValidationAsync(workerAlias, id, CancellationToken));

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
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
            Audit("job-cancel-requested", $"{workerAlias}/{job.Id}");
            return job;
        });

    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public Task<AppResponseBase<AIFineTuningWorkerDto>> SaveWorkerAsync([FromBody] AIFineTuningWorker_CreateOrEditRequest request) =>
        this.GetResponseAsync<AIFineTuningWorkerDto>(async (response, logger) =>
        {
            var worker = await _workerService.SaveAsync(request, _workerOptions.ConfiguredWorkerAliases, CancellationToken);
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
}
