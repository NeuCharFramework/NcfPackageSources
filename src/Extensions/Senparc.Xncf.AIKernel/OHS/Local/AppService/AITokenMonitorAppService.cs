/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenMonitorAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.Entities;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{
    [ApiAuthorize]
    public class AITokenMonitorAppService : AppServiceBase
    {
        private readonly AIModelService _aIModelService;
        private readonly AITokenUsageService _aITokenUsageService;
        private readonly AITokenMonitorService _aITokenMonitorService;

        /// <summary>
        /// Initializes token-usage monitoring and model lookup dependencies.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="aIModelService">The AI model service.</param>
        /// <param name="aITokenUsageService">The token usage query service.</param>
        /// <param name="aITokenMonitorService">The token monitor service.</param>
        public AITokenMonitorAppService(
            IServiceProvider serviceProvider,
            AIModelService aIModelService,
            AITokenUsageService aITokenUsageService,
            AITokenMonitorService aITokenMonitorService) : base(serviceProvider)
        {
            _aIModelService = aIModelService;
            _aITokenUsageService = aITokenUsageService;
            _aITokenMonitorService = aITokenMonitorService;
        }

        #region Legacy section

        /// <summary>
        /// Returns a paged token-usage list using the requested filters and ordering.
        /// </summary>
        /// <param name="request">The paging, filter, and ordering request.</param>
        /// <returns>A response containing token-usage DTOs and total count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PagedResponse<AITokenUsageDto>>> GetPagedListAsync(AITokenUsage_GetListRequest request)
        {
            return await this
                .GetResponseAsync<AppResponseBase<PagedResponse<AITokenUsageDto>>, PagedResponse<AITokenUsageDto>>(
                    async (response, logger) =>
                    {
                        var where = _aITokenUsageService.GetListWhere(request);

                        var list = await _aITokenUsageService.GetObjectListAsync(request.Page, request.Size, where, request.Order);
                        var total = await _aITokenUsageService.GetCountAsync(where);

                        return new PagedResponse<AITokenUsageDto>(
                            total,
                            list.Select(m => new AITokenUsageDto(m))
                        );
                    });
        }

        /// <summary>
        /// Returns all token-usage records matching the supplied filters.
        /// </summary>
        /// <param name="request">The filter and ordering request.</param>
        /// <returns>A response containing token-usage DTOs.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AITokenUsageDto>>> GetListAsync(AITokenUsage_GetListRequest request)
        {
            return await this.GetResponseAsync<List<AITokenUsageDto>>(
                async (response, logger) =>
                {
                    var where = _aITokenUsageService.GetListWhere(request);
                    var list = (await _aITokenUsageService.GetFullListAsync(where, request.Order))
                        .Select(m => new AITokenUsageDto(m))
                        .ToList();
                    return list;
                });
        }

        /// <summary>
        /// Loads one persisted token-usage record.
        /// </summary>
        /// <param name="id">The token-usage identifier.</param>
        /// <returns>A response containing the usage DTO.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AITokenUsageDto>> GetAsync(int id)
        {
            return await this.GetResponseAsync<AITokenUsageDto>(async (response, logger) =>
            {
                var entity = await _aITokenUsageService.GetObjectAsync(z => z.Id == id);
                return new AITokenUsageDto(entity);
            });
        }

        /// <summary>
        /// Deletes one persisted token-usage record.
        /// </summary>
        /// <param name="id">The token-usage identifier.</param>
        /// <returns>A response indicating whether deletion completed.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<bool>> DeleteAsync(int id)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                var entity = await _aITokenUsageService.GetObjectAsync(z => z.Id == id);
                if (entity == null)
                {
                    response.ErrorMessage = "当前实体已删除或不存在!";
                    response.Success = false;
                    return false;
                }
                await _aITokenUsageService.DeleteObjectAsync(entity);
                return true;
            });
        }

        #endregion

        #region Legacy section

        /// <summary>
        /// Returns aggregated token usage for the last seven days.
        /// </summary>
        /// <returns>A response containing persisted usage statistics.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenMonitorStats>> GetStatsAsync()
        {
            return await this.GetResponseAsync<AITokenMonitorStats>(async (response, logger) =>
            {
                var stats = await _aITokenUsageService.GetStatsAsync(7);
                return stats;
            });
        }

        /// <summary>
        /// Returns live in-memory token usage for the current tenant over the last seven days.
        /// </summary>
        /// <returns>A response containing live monitor statistics.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenMonitorStats>> GetLiveStatsAsync()
        {
            return await this.GetResponseAsync<AITokenMonitorStats>(async (response, logger) =>
            {
                return _aITokenMonitorService.GetLiveStats(7, TenantMonitorScope.GetTenantId(ServiceProvider));
            });
        }

        #endregion

        #region Legacy section

        /// <summary>
        /// Runs one configured AI model while recording live token progress.
        /// </summary>
        /// <param name="request">The model identifier, prompt, system message, and generation limits.</param>
        /// <returns>A response containing run output, usage, timing, and errors.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenMonitor_RunModelResponse>> RunModelWithMonitorAsync(AITokenMonitor_RunModelRequest request)
        {
            return await this.GetResponseAsync<AITokenMonitor_RunModelResponse>(async (response, logger) =>
            {
                var aiModel = await _aIModelService.GetObjectAsync(z => z.Id == request.ModelId)
                              ?? throw new Senparc.Ncf.Core.Exceptions.NcfExceptionBase("未查询到实体!");
                var aiModelDto = _aIModelService.Mapper.Map<AIModelDto>(aiModel);

                var promptConfigParameter = new PromptConfigParameter()
                {
                    MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : 2000,
                    Temperature = request.Temperature ?? 0.7f,
                    TopP = 0.5f
                };

                var runId = Guid.NewGuid();
                var progress = new Progress<AITokenProgressEvent>(evt =>
                {
                    logger.Append($"[{evt.Status}] 输入:{evt.InputTokens} 输出:{evt.OutputTokens} 总:{evt.TotalTokens} 耗时:{evt.ElapsedMs}ms");
                });

                var sw = System.Diagnostics.Stopwatch.StartNew();
                Senparc.AI.AgentKernel.SenparcKernelAiResult<string> result = null;
                string error = null;
                try
                {
                    result = await _aIModelService.RunModelWithMonitorAsync(
                        aiModelDto,
                        request.Prompt,
                        request.SystemMessage,
                        promptConfigParameter,
                        _aITokenMonitorService,
                        progress,
                        request.Source ?? "Monitor",
                        runId,
                        base.CancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                sw.Stop();

                var usage = AIModelService.BuildUsageSnapshot(result?.Result?.Usage);
                return new AITokenMonitor_RunModelResponse
                {
                    RunId = runId,
                    Success = error == null,
                    Output = result?.OutputString,
                    InputTokens = usage.InputTokens,
                    OutputTokens = usage.OutputTokens,
                    TotalTokens = usage.TotalTokens,
                    CachedInputTokens = usage.CachedInputTokens,
                    ReasoningTokens = usage.ReasoningTokens,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    Error = error
                };
            });
        }

        /// <summary>
        /// Returns the latest token-progress event for a monitored run and current tenant.
        /// </summary>
        /// <param name="request">The monitored run identifier.</param>
        /// <returns>A response containing the latest progress event.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenProgressEvent>> GetProgressAsync(AITokenMonitor_GetProgressRequest request)
        {
            return await this.GetResponseAsync<AITokenProgressEvent>(async (response, logger) =>
            {
                var progress = _aITokenMonitorService.GetLatestProgress(request.RunId, TenantMonitorScope.GetTenantId(ServiceProvider));
                return progress;
            });
        }

        /// <summary>
        /// Returns buffered token-progress events for a monitored run and current tenant.
        /// </summary>
        /// <param name="request">The monitored run identifier.</param>
        /// <returns>A response containing buffered progress events in sequence order.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AITokenProgressEvent>>> GetBufferedProgressAsync(AITokenMonitor_GetProgressRequest request)
        {
            return await this.GetResponseAsync<List<AITokenProgressEvent>>(async (response, logger) =>
            {
                var buffered = _aITokenMonitorService.GetBufferedProgress(request.RunId, TenantMonitorScope.GetTenantId(ServiceProvider));
                return buffered.ToList();
            });
        }

        #endregion
    }
}
