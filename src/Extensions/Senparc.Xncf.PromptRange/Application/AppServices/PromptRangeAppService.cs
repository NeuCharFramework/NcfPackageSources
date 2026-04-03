using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.Entities;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

/// <summary>
///PromptRange Management AppService
/// TODO: Permission verification required
/// </summary>
//[ApiAuthorize("AdminOnly")]
public class PromptRangeAppService : AppServiceBase
{
    private readonly PromptRangeService _promptRangeService;

    public PromptRangeAppService(
        IServiceProvider serviceProvider,
        PromptRangeService promptRangeService) : base(serviceProvider)
    {
        _promptRangeService = promptRangeService;
    }


    // /// <summary>
    // ///Set the AI ​​automatic scoring standard interface
    // /// </summary>
    // /// <param name="promptRangeId"></param>
    // /// <param name="expectedResults"></param>
    // /// <returns></returns>
    // [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    // public async Task<AppResponseBase<PromptRangeDto>> UpdateExpectedResults(int promptRangeId, string expectedResults)
    // {
    //     return await this.GetResponseAsync<PromptRangeDto>(
    //         async (response, logger) =>
    //             await _promptRangeService.UpdateExpectedResultsAsync(promptRangeId, expectedResults)
    //     );
    // }

    /// <summary>
    /// Get shooting range list details (add reverse chronological order)
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<PromptRangeDto>> AddAsync(string alias)
    {
        return await this.GetResponseAsync<PromptRangeDto>(
            async (response, logger) =>
                await _promptRangeService.AddAsync(alias)
        );
    }

    /// <summary>
    /// Get shooting range list details (add reverse chronological order)
    /// </summary>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
    public async Task<AppResponseBase<List<PromptRangeDto>>> GetListAsync()
    {
        return await this.GetResponseAsync<List<PromptRangeDto>>(
            async (response, logger) =>
                await _promptRangeService.GetListAsync()
        );
    }


    /// <summary>
    /// Get the tree structure of PromptRange
    /// </summary>
    /// <returns></returns>
    [ApiBind]
    public async Task<AppResponseBase<PromptItemTreeList>> GetPromptRangeTree()
    {
        return await this.GetResponseAsync<PromptItemTreeList>(async (response, logger) =>
        {
            var promptItemService = base.GetService<PromptItemService>();
            var items = await promptItemService.GetPromptRangeTreeList(true, true);
            return items;
        });
    }

    /// <summary>
    /// Modify custom shooting range code
    /// </summary>
    /// <param name="rangeId"></param>
    /// <param name="alias"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
    public async Task<AppResponseBase<PromptRangeDto>> ChangeAliasAsync(int rangeId, string alias)
    {
        return await this.GetResponseAsync<PromptRangeDto>(
            async (response, logger) =>
                await _promptRangeService.ChangeAliasAsync(rangeId, alias)
        );
    }

    /// <summary>
    /// Delete range based on ID
    /// </summary>
    /// <param name="rangeId"></param>
    /// <returns></returns>
    [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
    public async Task<StringAppResponse> DeleteAsync(int rangeId)
    {
        return await this.GetStringResponseAsync(
            async (response, logger) =>
            {
                var status = await _promptRangeService.DeleteAsync(rangeId);
                if (status)
                {
                    return "ok";
                }

                throw new NcfExceptionBase("删除失败");
            }
        );
    }
}