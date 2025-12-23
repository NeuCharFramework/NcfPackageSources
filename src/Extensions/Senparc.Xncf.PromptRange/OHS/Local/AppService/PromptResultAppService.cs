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
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
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
        /// <param name="userMessage">用户消息（可选，如果为空且第一个结果是 Chat 模式，则从第一个结果的对话记录中获取）</param>
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
                    
                    // 连发时，如果第一个结果是 Chat 模式，后续结果也需要保持 Chat 模式
                    // 如果没有传入 userMessage，先检查该 PromptItem 的第一个 PromptResult 是否是 Chat 模式
                    string firstUserMessage = userMessage;
                    if (string.IsNullOrWhiteSpace(firstUserMessage))
                    {
                        // 获取该 PromptItem 的第一个 PromptResult（按 ID 升序）
                        var existingResults = await _promptResultService.GetByItemId(promptItemId);
                        if (existingResults != null && existingResults.Count > 0)
                        {
                            var firstExistingResult = existingResults.OrderBy(r => r.Id).FirstOrDefault();
                            if (firstExistingResult != null && firstExistingResult.Mode == ResultMode.Chat)
                            {
                                // 第一个结果是 Chat 模式，从对话记录中获取第一条用户消息
                                var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(firstExistingResult.Id);
                                var firstUserChat = chatHistory?.FirstOrDefault(c => c.RoleType == ChatRoleType.User);
                                if (firstUserChat != null && !string.IsNullOrWhiteSpace(firstUserChat.Content))
                                {
                                    firstUserMessage = firstUserChat.Content;
                                }
                            }
                        }
                    }
                    
                    // 获取第一个结果的模式，用于后续结果保持一致
                    ResultMode? firstResultMode = null;
                    
                    for (int i = 0; i < numsOfResults; i++)
                    {
                        // 如果是第一次生成，使用传入的参数
                        // 如果是后续生成，且第一个结果是 Chat 模式，则保持 Chat 模式
                        string currentUserMessage = null;
                        List<ChatMessageDto> currentChatHistory = null;
                        
                        if (i == 0)
                        {
                            // 第一次生成，使用传入的参数
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; // 第一次生成时，chatHistory 应该为空
                        }
                        else if (firstResultMode == ResultMode.Chat && !string.IsNullOrWhiteSpace(firstUserMessage))
                        {
                            // 后续生成，且第一个结果是 Chat 模式，保持 Chat 模式
                            // 使用相同的 userMessage，但不传递历史记录（每次都是独立的对话）
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; // 连发时，每次都是独立的对话，不传递历史记录
                        }
                        // 如果第一个结果是 Single 模式，后续也使用 Single 模式（currentUserMessage 为 null）
                        
                        var result = await _promptResultService.SenparcGenerateResultAsync(promptItem, currentUserMessage, currentChatHistory);
                        
                        // 记录第一个结果的模式
                        if (i == 0)
                        {
                            firstResultMode = result.Mode;
                            firstUserMessage = currentUserMessage;
                        }
                        
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
        /// 根据 PromptResultId 获取对话历史和 Prompt 内容
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ChatHistoryWithPromptResponse>> GetChatHistoryWithPrompt(int promptResultId)
        {
            return await this.GetResponseAsync<PromptResult_ChatHistoryWithPromptResponse>(
                async (response, logger) =>
                {
                    // 获取对话历史
                    var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
                    
                    // 获取 PromptResult
                    var promptResult = await _promptResultService.GetObjectAsync(p => p.Id == promptResultId)
                        ?? throw new NcfExceptionBase($"未找到 ID 为 {promptResultId} 的 PromptResult");
                    
                    // 优先使用保存的 SystemMessage，如果没有则使用当前的 Prompt 内容
                    string promptContent;
                    if (!string.IsNullOrWhiteSpace(promptResult.SystemMessage))
                    {
                        // 使用保存的 SystemMessage（已完成参数替换）
                        promptContent = promptResult.SystemMessage;
                    }
                    else
                    {
                        // 降级方案：如果没有保存的 SystemMessage，使用当前的 Prompt 内容
                        var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId);
                        promptContent = promptItem.Content ?? string.Empty;
                    }
                    
                    return new PromptResult_ChatHistoryWithPromptResponse
                    {
                        ChatHistory = chatHistory,
                        PromptContent = promptContent
                    };
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
        /// <param name="request">更新反馈请求</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptResultChatDto>> UpdateChatFeedback(PromptResult_UpdateChatFeedbackRequest request)
        {
            return await this.GetResponseAsync<PromptResultChatDto>(
                async (response, logger) =>
                {
                    return await _promptResultChatService.UpdateUserFeedbackAsync(request.ChatId, request.Feedback);
                });
        }
    }
}