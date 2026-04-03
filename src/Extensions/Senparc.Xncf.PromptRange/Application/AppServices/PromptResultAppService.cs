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
    /// <summary>
    ///PromptResult Management AppService
    /// TODO: Permission verification required
    /// </summary>
    //[ApiAuthorize("AdminOnly")]
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
        /// Manual scoring
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

                    // Update the score of the bound item
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
        /// automatic scoring
        /// Accept a promptItemId, then find all promptResults, and then score
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
        /// Get the corresponding result list based on the target channel ID
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
        /// generate results
        /// </summary>
        /// <param name="promptItemId">Target ID</param>
        /// <param name="numsOfResults">Number of bursts</param>
        /// <param name="userMessage">User message (optional, if empty and the first result is Chat mode, obtained from the conversation record of the first result)</param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemId(int promptItemId, int numsOfResults,string userMessage=null)
        {
            return await this.GetResponseAsync<PromptResult_ListResponse>(
                async (response, logger) =>
                {
                    var promptItem = await _promptItemService.DraftSwitch(promptItemId, false);

                    // #region Delete previous results
                    //
                    // var delSucFrag = await _promptResultService.BatchDeleteWithItemId(promptItemId);
                    // if (!delSucFrag)
                    // {
                    //     throw new NcfExceptionBase("Deletion failed");
                    // }
                    //
                    // #endregion


                    var resp = new PromptResult_ListResponse(promptItemId, promptItem, new());
                    
                    // When sending continuously, if the first result is in Chat mode, subsequent results also need to remain in Chat mode.
                    // If no userMessage is passed in, first check whether the first PromptResult of the PromptItem is in Chat mode.
                    string firstUserMessage = userMessage;
                    if (string.IsNullOrWhiteSpace(firstUserMessage))
                    {
                        // Get the first PromptResult of this PromptItem (in ascending order by ID)
                        var existingResults = await _promptResultService.GetByItemId(promptItemId);
                        if (existingResults != null && existingResults.Count > 0)
                        {
                            var firstExistingResult = existingResults.OrderBy(r => r.Id).FirstOrDefault();
                            if (firstExistingResult != null && firstExistingResult.Mode == ResultMode.Chat)
                            {
                                // The first result is Chat mode, getting the first user message from the conversation record
                                var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(firstExistingResult.Id);
                                var firstUserChat = chatHistory?.FirstOrDefault(c => c.RoleType == ChatRoleType.User);
                                if (firstUserChat != null && !string.IsNullOrWhiteSpace(firstUserChat.Content))
                                {
                                    firstUserMessage = firstUserChat.Content;
                                }
                            }
                        }
                    }
                    
                    // Get the pattern of the first result to keep subsequent results consistent
                    ResultMode? firstResultMode = null;
                    
                    for (int i = 0; i < numsOfResults; i++)
                    {
                        // If it is generated for the first time, use the parameters passed in
                        // If it is a subsequent build and the first result is in Chat mode, stay in Chat mode
                        string currentUserMessage = null;
                        List<ChatMessageDto> currentChatHistory = null;
                        
                        if (i == 0)
                        {
                            // The first generation, using the parameters passed in
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; // The first time it is generated, chatHistory should be empty
                        }
                        else if (firstResultMode == ResultMode.Chat && !string.IsNullOrWhiteSpace(firstUserMessage))
                        {
                            // Subsequent generation, and the first result is in Chat mode, remains in Chat mode
                            // Use the same userMessage but don't pass the history (a separate conversation each time)
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; // When sending continuously, each session is an independent conversation, and no history records are transferred.
                        }
                        // If the first result is Single mode, subsequent Single modes are also used (currentUserMessage is null)
                        
                        var result = await _promptResultService.SenparcGenerateResultAsync(promptItem, currentUserMessage, currentChatHistory);
                        
                        // Pattern to record the first result
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
        /// Get the conversation record based on PromptResultId
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
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
        /// Get the conversation history and Prompt content based on PromptResultId
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ChatHistoryWithPromptResponse>> GetChatHistoryWithPrompt(int promptResultId)
        {
            return await this.GetResponseAsync<PromptResult_ChatHistoryWithPromptResponse>(
                async (response, logger) =>
                {
                    // Get conversation history
                    var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
                    
                    // GetPromptResult
                    var promptResult = await _promptResultService.GetObjectAsync(p => p.Id == promptResultId)
                        ?? throw new NcfExceptionBase($"未找到 ID 为 {promptResultId} 的 PromptResult");
                    
                    // The saved SystemMessage is used first, if not, the current Prompt content is used.
                    string promptContent;
                    if (!string.IsNullOrWhiteSpace(promptResult.SystemMessage))
                    {
                        // Use saved SystemMessage (parameter substitution done)
                        promptContent = promptResult.SystemMessage;
                    }
                    else
                    {
                        // Downgrade scenario: If there is no saved SystemMessage, use the current Prompt content
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
        /// Continue chatting: Append the conversation record to the existing PromptResult
        /// </summary>
        /// <param name="request">Continue chat request</param>
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
        /// Update user feedback of conversation records (Like/Unlike)
        /// </summary>
        /// <param name="request">Update feedback request</param>
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