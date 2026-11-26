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

        /// <summary>
        /// Initializes the application service used to score, generate, inspect, and continue prompt results.
        /// </summary>
        /// <param name="serviceProvider">The dependency-injection provider used by the base application service.</param>
        /// <param name="promptResultService">Service for prompt-result persistence and AI execution.</param>
        /// <param name="promptItemService">Service for resolving prompt definitions.</param>
        /// <param name="promptResultChatService">Service for chat-history persistence and feedback.</param>
        /// <param name="promptResultStreamHub">Hub used to publish optional generation stream events.</param>
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
        /// Stores a human score for a generated result and recalculates the parent prompt item's aggregate score.
        /// </summary>
        /// <param name="request">The result identifier and human score.</param>
        /// <returns>A string response indicating whether scoring completed.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the result does not exist or the score is invalid.</exception>
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
        /// Calculates an automated score for a generated result using the supplied expected-result values.
        /// </summary>
        /// <param name="promptResultId">The generated-result identifier.</param>
        /// <param name="expectedResultList">The expected-result values used by the evaluator.</param>
        /// <param name="isRefresh">Whether an existing automated score may be recalculated.</param>
        /// <returns>A string response indicating whether scoring completed.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the result, expected values, or scoring state is invalid.</exception>
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
        /// Returns the generated results and prompt metadata for one prompt item.
        /// </summary>
        /// <param name="promptItemId">The prompt-item identifier.</param>
        /// <returns>A response containing the prompt item and its generated results.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the prompt item cannot be found.</exception>
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
        /// Generates one or more results for a prompt item and optionally publishes stream events.
        /// </summary>
        /// <param name="promptItemId">The prompt-item identifier.</param>
        /// <param name="numsOfResults">The number of results to generate.</param>
        /// <param name="userMessage">Optional user input for the generation.</param>
        /// <param name="streamId">Optional stream identifier used to receive progress events.</param>
        /// <returns>A response containing the generated result list.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the prompt item or generation configuration is invalid.</exception>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptResult_ListResponse>> GenerateWithItemId(int promptItemId, int numsOfResults, string userMessage = null, string streamId = null)
        {
            return await GenerateWithItemIdInternal(promptItemId, numsOfResults, userMessage, streamId, null);
        }

        /// <summary>
        /// Generates prompt results from a request object, including optional execution options.
        /// </summary>
        /// <param name="request">The prompt identifier, result count, user message, stream identifier, and options.</param>
        /// <returns>A response containing the generated result list.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the prompt item or generation configuration is invalid.</exception>
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


        /// <summary>
        /// Recalculates the aggregate evaluation score for a prompt item from its scored results.
        /// </summary>
        /// <param name="promptItemId">The prompt-item identifier to recalculate.</param>
        /// <returns>A string response indicating whether recalculation completed.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the prompt item cannot be found.</exception>
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
        /// Loads the persisted chat messages associated with a generated result.
        /// </summary>
        /// <param name="promptResultId">The generated-result identifier.</param>
        /// <returns>A response containing the stored chat history.</returns>
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
        /// Loads a generated result's chat history together with the prompt content used to produce it.
        /// </summary>
        /// <param name="promptResultId">The generated-result identifier.</param>
        /// <returns>A response containing chat history and prompt content.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the generated result cannot be found.</exception>
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
        /// Continues an existing chat result with a new user message and optionally publishes stream events.
        /// </summary>
        /// <param name="request">The result identifier, user message, and optional stream identifier.</param>
        /// <returns>A response containing the updated chat messages.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the source result or continuation configuration is invalid.</exception>
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
        /// Searches the vector index associated with a prompt result using the supplied query and result limit.
        /// </summary>
        /// <param name="request">The prompt result identifier, query text, and optional top-k limit.</param>
        /// <returns>A response containing matching embedding-search entries.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the result, query, or vector configuration is invalid.</exception>
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
        /// Stores user feedback for one chat message and returns the updated message DTO.
        /// </summary>
        /// <param name="request">The chat-message identifier and feedback value.</param>
        /// <returns>A response containing the updated chat message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the chat message cannot be found or feedback is invalid.</exception>
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
