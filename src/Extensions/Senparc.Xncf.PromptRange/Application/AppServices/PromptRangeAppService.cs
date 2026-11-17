/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptRangeAppService.cs
    File description: PromptRangeAppService related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.Domain.Models.Entities;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.CO2NET.Extensions;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

/// <summary>
/// PromptRange management AppService
/// TODO: permission validation is required
/// </summary>
[ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
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
    //Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    /// according to　ID deleterange
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

    //[ApiBind]
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [FunctionRender(typeof(PromptRangeResource), "Function.PromptRange.ViewCodes.Name", "Function.PromptRange.ViewCodes.Description", typeof(Register))]
    public async Task<StringAppResponse> ViewPromptCodeList(PromptRange_ViewPromptCodeRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var promptItemService = base.GetService<PromptItemService>();
            var filterRangeName = request?.FilterRangeName?.Trim();
            var tree = await promptItemService.GetPromptRangeTreeList(true, true, filterRangeName);

            logger.Append("=== PromptCode 列表（可用于在 AgentsManager 中创建智能体）===");
            logger.Append("");
            logger.Append($"筛选条件：{(filterRangeName.IsNullOrEmpty() ? "全部靶场" : filterRangeName)}");
            logger.Append("");
            logger.Append("覆盖范围说明：");
            logger.Append("  靶场级别（Range）：使用靶场名称作为 PromptCode，匹配该靶场下的最优 Prompt");
            logger.Append("  靶道级别（Tactic）：使用「靶场名称-T战术编号」，匹配该战术下的最优 Prompt");
            logger.Append("  完整定位（Full）：使用完整版本号，精确匹配指定 Prompt");
            logger.Append("");

            foreach (var item in tree)
            {
                logger.Append($"[{item.Level}] {item.Text}  →  PromptCode: {item.Value}");
            }

            logger.Append("");
            logger.Append("提示：在 AgentsManager 模块中，使用[从 PromptCode 快速创建智能体]功能可基于以上 PromptCode 快速创建智能体。");

            return logger.ToString();
        });
    }
}
