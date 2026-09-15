/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AITokenMonitorAppService.cs
    文件功能描述：AITokenMonitorAppService Token 监控相关实现
    
    创建标识：Senparc - 20260904

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

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

        #region 分页 / 列表 / 单条

        /// <summary>
        /// 分页获取 Token 使用记录
        /// </summary>
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
        /// 获取 Token 使用记录列表
        /// </summary>
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
        /// 获取单条 Token 使用记录
        /// </summary>
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
        /// 删除一条 Token 使用记录
        /// </summary>
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

        #region 统计

        /// <summary>
        /// 获取 Token 监控聚合统计（基于持久化记录）
        /// </summary>
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
        /// 获取 Token 监控实时统计（基于进程内实时聚合）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenMonitorStats>> GetLiveStatsAsync()
        {
            return await this.GetResponseAsync<AITokenMonitorStats>(async (response, logger) =>
            {
                return _aITokenMonitorService.GetLiveStats(7);
            });
        }

        #endregion

        #region 运行 + 异步进度

        /// <summary>
        /// 运行指定模型并记录 Token 监控（运行过程中可通过 GetProgressAsync 查询异步进度）
        /// </summary>
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
        /// 查询某次运行的最新异步进度
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AITokenProgressEvent>> GetProgressAsync(AITokenMonitor_GetProgressRequest request)
        {
            return await this.GetResponseAsync<AITokenProgressEvent>(async (response, logger) =>
            {
                var progress = _aITokenMonitorService.GetLatestProgress(request.RunId);
                return progress;
            });
        }

        /// <summary>
        /// 查询某次运行的全部缓冲进度（用于重放）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AITokenProgressEvent>>> GetBufferedProgressAsync(AITokenMonitor_GetProgressRequest request)
        {
            return await this.GetResponseAsync<List<AITokenProgressEvent>>(async (response, logger) =>
            {
                var buffered = _aITokenMonitorService.GetBufferedProgress(request.RunId);
                return buffered.ToList();
            });
        }

        #endregion
    }
}
