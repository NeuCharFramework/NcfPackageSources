using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptResultAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptResult> _promptResultRepository;
        private readonly PromptResultService _promptResultService;
        private readonly PromptItemService _promptItemService;

        public PromptResultAppService(
            IServiceProvider serviceProvider,
            PromptResultService promptResultService,
            PromptItemService promptItemService) : base(serviceProvider)
        {
            _promptResultService = promptResultService;
            _promptItemService = promptItemService;
        }

        /// <summary>
        /// 手动打分
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> HumanScore(PromptResult_HumanScoreRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    PromptResult result = await _promptResultService.ManualScoreAsync(request.PromptResultId, request.HumanScore);

                    // 更新绑定的 item 的分数
                    await _promptResultService.UpdateEvalScoreAsync(result.PromptItemId);

                    return "ok";

                    // var result = await _promptResultService.GetObjectAsync(p => p.Id == request.PromptResultId);
                    //
                    // result.ManualScoring(request.HumanScore);
                    //
                    // await _promptResultService.SaveObjectAsync(result);
                    //
                    // return "ok";
                });
        }

        /// <summary>
        /// 自动打分
        /// 接受一个promptItemId，然后找到所有的promptResult，然后进行评分
        /// </summary>
        /// <param name="promptResultId"></param>
        /// <param name="expectedResultList"></param>
        /// <param name="isRefresh"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> RobotScore(int promptResultId, List<string> expectedResultList, bool isRefresh = false)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    #region Validate

                    if (expectedResultList == null || expectedResultList.Count == 0)
                    {
                        throw new NcfExceptionBase("期望结果为空时不能自动打分");
                    }

                    #endregion

                    var promptResult = await _promptResultService.RobotScoringAsync(promptResultId, isRefresh, expectedResultList);

                    await _promptResultService.UpdateEvalScoreAsync(promptResult.PromptItemId);

                    return "ok";
                });
        }

        /// <summary>
        /// 根据靶道ID获取对应的结果列表
        /// </summary>
        /// <param name="promptItemId"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GetByItemId(int promptItemId)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptResult_ListResponse>, PromptResult_ListResponse>(
                async (response, logger) =>
                {
                    var result = await _promptResultService.GetByItemId(promptItemId);

                    var item = await _promptItemService.GetAsync(promptItemId);

                    return new PromptResult_ListResponse(promptItemId, item, result);
                });
        }

        /// <summary>
        /// 生成结果
        /// </summary>
        /// <param name="promptItemId">靶道ID</param>
        /// <param name="numsOfResults">连发次数</param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemId(int promptItemId, int numsOfResults)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptResult_ListResponse>, PromptResult_ListResponse>(
                async (response, logger) =>
                {
                    var promptItem = await _promptItemService.DraftSwitch(promptItemId, false);
                    
                    // #region 删除之前的结果
                    //
                    // var delSucFrag = await _promptResultService.BatchDeleteWithItemId(promptItemId);
                    // if (!delSucFrag)
                    // {
                    //     throw new NcfExceptionBase("删除失败");
                    // }
                    //
                    // #endregion


                    var resp = new PromptResult_ListResponse(promptItemId, promptItem, new());
                    for (int i = 0; i < numsOfResults; i++)
                    {
                        var result = await _promptResultService.SenparcGenerateResultAsync(promptItem);
                        resp.PromptResults.Add(result);
                    }

                    await _promptResultService.UpdateEvalScoreAsync(promptItemId);

                    return resp;
                }
            );
        }


        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<StringAppResponse> ReCalculateItemScore(int promptItemId)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    await _promptResultService.UpdateEvalScoreAsync(promptItemId);
                    return "ok";
                });
        }
    }
}