using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

public class PromptRangeAppService : AppServiceBase
{
    private readonly PromptRangeService _promptRangeService;

    public PromptRangeAppService(
        IServiceProvider serviceProvider,
        PromptRangeService promptRangeService) : base(serviceProvider)
    {
        _promptRangeService = promptRangeService;
    }


    /// <summary>
    /// 设置 AI 自动打分评分标准接口
    /// </summary>
    /// <param name="promptRangeId"></param>
    /// <param name="expectedResults"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<PromptRangeDto>> UpdateExpectedResults(int promptRangeId, string expectedResults)
    {
        return await this.GetResponseAsync<AppResponseBase<PromptRangeDto>, PromptRangeDto>(
            async (response, logger) =>
                await _promptRangeService.UpdateExpectedResultsAsync(promptRangeId, expectedResults)
        );
    }

    /// <summary>
    /// 获取靶场列表详情（添加时间倒序）
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<PromptRangeDto>> AddAsync(PromptRange_AddRequest request)
    {
        return await this.GetResponseAsync<AppResponseBase<PromptRangeDto>, PromptRangeDto>(
            async (response, logger) =>
                await _promptRangeService.AddAsync(request)
        );
    }

    /// <summary>
    /// 获取靶场列表详情（添加时间倒序）
    /// </summary>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<List<PromptRangeDto>>> GetListAsync()
    {
        return await this.GetResponseAsync<AppResponseBase<List<PromptRangeDto>>, List<PromptRangeDto>>(
            async (response, logger) => await _promptRangeService.GetListAsync());
    }

    /// <summary>
    /// 修改自定义靶场代号
    /// </summary>
    /// <param name="rangeId"></param>
    /// <param name="alias"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<PromptRangeDto>> ChangeAliasAsync(int rangeId, string alias)
    {
        return await this.GetResponseAsync<AppResponseBase<PromptRangeDto>, PromptRangeDto>(
            async (response, logger) =>
                await _promptRangeService.ChangeAliasAsync(rangeId, alias)
        );
    }
}