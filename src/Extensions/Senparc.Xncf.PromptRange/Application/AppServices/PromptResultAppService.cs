/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20231120

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    /// PromptResult management AppService
    /// TODO: permission validation is required
    /// </summary>
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class PromptResultAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptResult> _promptResultRepository;
        private readonly PromptResultService _promptResultService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultChatService _promptResultChatService;
        private readonly PromptResultStreamHub _promptResultStreamHub;

        public PromptResultAppService(
            IServiceProvider serviceProvider,
            PromptResultService promptResultService,
            PromptItemService promptItemService,
            PromptResultChatService promptResultChatService,
            PromptResultStreamHub promptResultStreamHub) : base(serviceProvider)
        {
            _promptResultService = promptResultService;
            _promptItemService = promptItemService;
            _promptResultChatService = promptResultChatService;
            _promptResultStreamHub = promptResultStreamHub;
        }

        /// <summary>
        /// manualscoring
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

                    //Legacy comment translated to English.
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
        /// automaticscoring
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        /// generateresult
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemId(int promptItemId, int numsOfResults, string userMessage = null, string streamId = null)
        {
            return await GenerateWithItemIdInternal(promptItemId, numsOfResults, userMessage, streamId, null);
        }

        /// <summary>
        /// generateresult（POST version，supportextensionexecuteparameters）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemIdPost(PromptResult_GenerateRequest request)
        {
            request ??= new PromptResult_GenerateRequest();
            return await GenerateWithItemIdInternal(
                request.PromptItemId,
                request.NumsOfResults <= 0 ? 1 : request.NumsOfResults,
                request.UserMessage,
                request.StreamId,
                request.ExecutionOptions);
        }

        private async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemIdInternal(
            int promptItemId,
            int numsOfResults,
            string userMessage,
            string streamId,
            PromptExecutionOptions executionOptions)
        {
            return await this.GetResponseAsync<PromptResult_ListResponse>(
                async (response, logger) =>
                {
                    var promptItem = await _promptItemService.DraftSwitch(promptItemId, false);

                    // #region Legacy section
                    //
                    // var delSucFrag = await _promptResultService.BatchDeleteWithItemId(promptItemId);
                    // if (!delSucFrag)
                    // {
                    //     throw new NcfExceptionBase("deletefailure");
                    // }
                    //
                    // #endregion


                    var resp = new PromptResult_ListResponse(promptItemId, promptItem, new());
                    
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    string firstUserMessage = userMessage;
                    if (string.IsNullOrWhiteSpace(firstUserMessage))
                    {
                        //Legacy comment translated to English.
                        var existingResults = await _promptResultService.GetByItemId(promptItemId);
                        if (existingResults != null && existingResults.Count > 0)
                        {
                            var firstExistingResult = existingResults.OrderBy(r => r.Id).FirstOrDefault();
                            if (firstExistingResult != null && firstExistingResult.Mode == ResultMode.Chat)
                            {
                                //Legacy comment translated to English.
                                var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(firstExistingResult.Id);
                                var firstUserChat = chatHistory?.FirstOrDefault(c => c.RoleType == ChatRoleType.User);
                                if (firstUserChat != null && !string.IsNullOrWhiteSpace(firstUserChat.Content))
                                {
                                    firstUserMessage = firstUserChat.Content;
                                }
                            }
                        }
                    }
                    
                    //Legacy comment translated to English.
                    ResultMode? firstResultMode = null;
                    Action<PromptResultStreamEvent> streamCallback = null;
                    if (!string.IsNullOrWhiteSpace(streamId))
                    {
                        streamCallback = streamEvent =>
                        {
                            if (streamEvent == null)
                            {
                                return;
                            }
                            streamEvent.StreamId = streamId;
                            _promptResultStreamHub.Publish(streamEvent);
                        };
                    }
                    
                    for (int i = 0; i < numsOfResults; i++)
                    {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        string currentUserMessage = null;
                        List<ChatMessageDto> currentChatHistory = null;
                        
                        if (i == 0)
                        {
                            //Legacy comment translated to English.
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; //Legacy comment translated to English.
                        }
                        else if (firstResultMode == ResultMode.Chat && !string.IsNullOrWhiteSpace(firstUserMessage))
                        {
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; //Legacy comment translated to English.
                        }
                        //Legacy comment translated to English.
                        
                        var result = await _promptResultService.SenparcGenerateResultAsync(
                            promptItem,
                            currentUserMessage,
                            currentChatHistory,
                            streamCallback,
                            executionOptions);
                        
                        //Legacy comment translated to English.
                        if (i == 0)
                        {
                            firstResultMode = result.Mode;
                            firstUserMessage = currentUserMessage;
                        }
                        
                        resp.PromptResults.Add(result);
                    }

                    if (!string.IsNullOrWhiteSpace(streamId))
                    {
                        _promptResultStreamHub.Publish(new PromptResultStreamEvent
                        {
                            StreamId = streamId,
                            EventType = "complete",
                            IsFinal = true
                        });
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="promptResultId">PromptResult ID</param>
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="promptResultId">PromptResult ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ChatHistoryWithPromptResponse>> GetChatHistoryWithPrompt(int promptResultId)
        {
            return await this.GetResponseAsync<PromptResult_ChatHistoryWithPromptResponse>(
                async (response, logger) =>
                {
                    //Legacy comment translated to English.
                    var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
                    
                    // get PromptResult
                    var promptResult = await _promptResultService.GetObjectAsync(p => p.Id == promptResultId)
                        ?? throw new NcfExceptionBase($"未找到 ID 为 {promptResultId} 的 PromptResult");
                    
                    //Legacy comment translated to English.
                    string promptContent;
                    if (!string.IsNullOrWhiteSpace(promptResult.SystemMessage))
                    {
                        //Legacy comment translated to English.
                        promptContent = promptResult.SystemMessage;
                    }
                    else
                    {
                        //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<PromptResultChatDto>>> ContinueChat(PromptResult_ContinueChatRequest request)
        {
            return await this.GetResponseAsync<List<PromptResultChatDto>>(
                async (response, logger) =>
                {
                    Action<PromptResultStreamEvent> streamCallback = null;
                    if (!string.IsNullOrWhiteSpace(request.StreamId))
                    {
                        streamCallback = streamEvent =>
                        {
                            if (streamEvent == null)
                            {
                                return;
                            }
                            streamEvent.StreamId = request.StreamId;
                            _promptResultStreamHub.Publish(streamEvent);
                        };
                    }

                    var result = await _promptResultService.ContinueChatAsync(
                        request.PromptResultId,
                        request.UserMessage,
                        streamCallback);

                    if (!string.IsNullOrWhiteSpace(request.StreamId))
                    {
                        _promptResultStreamHub.Publish(new PromptResultStreamEvent
                        {
                            StreamId = request.StreamId,
                            EventType = "complete",
                            IsFinal = true
                        });
                    }

                    return result;
                });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptResult_TextEmbeddingSearchResponse>> TextEmbeddingSearch(PromptResult_TextEmbeddingSearchRequest request)
        {
            return await this.GetResponseAsync<PromptResult_TextEmbeddingSearchResponse>(
                async (response, logger) =>
                {
                    request ??= new PromptResult_TextEmbeddingSearchRequest();
                    return await _promptResultService.TextEmbeddingSearchAsync(
                        request.PromptResultId,
                        request.Query,
                        request.TopK);
                });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
