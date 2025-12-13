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
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptResultAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptResult> _promptResultRepository;
        private readonly PromptResultService _promptResultService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultChatService _promptResultChatService;

        public PromptResultAppService(
            IServiceProvider serviceProvider,
            PromptResultService promptResultService,
            PromptItemService promptItemService,
            PromptResultChatService promptResultChatService) : base(serviceProvider)
        {
            _promptResultService = promptResultService;
            _promptItemService = promptItemService;
            _promptResultChatService = promptResultChatService;
        }

        /// <summary>
        /// 手动打分
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> HumanScore(PromptResult_HumanScoreRequest request)
        {
            return await this.GetStringResponseAsync(
                async (response, logger) =>
                {
                    var result = await _promptResultService.ManualScoreAsync(request.PromptResultId, request.HumanScore);

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
            return await this.GetStringResponseAsync(
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
            return await this.GetResponseAsync<PromptResult_ListResponse>(
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
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemId(int promptItemId, int numsOfResults,string userMessage=null)
        {
            return await this.GetResponseAsync<PromptResult_ListResponse>(
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
                        var result = await _promptResultService.SenparcGenerateResultAsync(promptItem, userMessage);
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
            return await this.GetStringResponseAsync(
                async (response, logger) =>
                {
                    await _promptResultService.UpdateEvalScoreAsync(promptItemId);
                    return "ok";
                });
        }

        /// <summary>
        /// 根据 PromptResultId 获取对话记录
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<List<PromptResultChatDto>>> GetChatHistory(int promptResultId)
        {
            return await this.GetResponseAsync<List<PromptResultChatDto>>(
                async (response, logger) =>
                {
                    return await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
                });
        }

        /// <summary>
        /// 继续聊天：在现有 PromptResult 中追加对话记录
        /// </summary>
        /// <param name="request">继续聊天请求</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<PromptResultChatDto>>> ContinueChat(PromptResult_ContinueChatRequest request)
        {
            return await this.GetResponseAsync<List<PromptResultChatDto>>(
                async (response, logger) =>
                {
                    return await _promptResultService.ContinueChatAsync(request.PromptResultId, request.UserMessage);
                });
        }

        /// <summary>
        /// 更新对话记录的用户反馈（Like/Unlike）
        /// </summary>
        /// <param name="chatId">对话记录 ID</param>
        /// <param name="feedback">Like（true）、Unlike（false）、取消反馈（null）</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptResultChatDto>> UpdateChatFeedback(int chatId, bool? feedback)
        {
            return await this.GetResponseAsync<PromptResultChatDto>(
                async (response, logger) =>
                {
                    return await _promptResultChatService.UpdateUserFeedbackAsync(chatId, feedback);
                });
        }
    }
}