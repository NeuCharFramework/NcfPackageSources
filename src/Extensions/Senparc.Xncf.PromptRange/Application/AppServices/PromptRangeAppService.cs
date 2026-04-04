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

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService;

/// <summary>
/// PromptRange 管理 AppService
/// TODO: 需要权限验证
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
    // /// 设置 AI 自动打分评分标准接口
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
    /// 获取靶场列表详情（添加时间倒序）
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
    /// 获取靶场列表详情（添加时间倒序）
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
    /// 获取 PromptRange 的树状结构
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
    /// 修改自定义靶场代号
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
    /// 根据　ID 删除靶场
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
    /// FunctionRender：查看靶场 PromptCode 列表（用于创建智能体）
    /// </summary>
    [FunctionRender("查看 PromptCode 列表", "查看所有靶场和靶道的 PromptCode，可用于在 AgentsManager 中快速创建智能体", typeof(Register))]
    public async Task<StringAppResponse> ViewPromptCodeList(PromptRange_ViewPromptCodeRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var promptItemService = base.GetService<PromptItemService>();
            var tree = await promptItemService.GetPromptRangeTreeList(true, true);

            logger.Append("=== PromptCode 列表（可用于在 AgentsManager 中创建智能体）===");
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