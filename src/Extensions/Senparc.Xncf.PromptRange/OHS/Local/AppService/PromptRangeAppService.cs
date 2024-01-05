using System;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

public class PromptRangeAppService : AppServiceBase
{
    private readonly PromptRangeService _promptRangeService;

    public PromptRangeAppService(IServiceProvider serviceProvider, PromptRangeService promptRangeService) : base(serviceProvider)
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
                await _promptRangeService.UpdateExpectedResultsAsync(promptRangeId, expectedResults));
    }
}