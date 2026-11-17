/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: PromptItemAppService.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20231021
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261102
    Change description: Updated module behavior and related support.

    Change description: v0.16.4-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 add PromptRange selector service, UI components, and tests

    Modified by: Senparc - 20261105
    Change description: v0.20.14 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using Senparc.Xncf.PromptRange.Abstractions.References;


namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    /// PromptItem management AppService
    /// TODO: permission validation is required
    /// </summary>
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class PromptItemAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;
        private readonly PromptResultStreamHub _promptResultStreamHub;

        /// <inheritdoc />
        public PromptItemAppService(IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptResultService promptResultService,
            PromptResultStreamHub promptResultStreamHub) : base(serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptResultService = promptResultService;
            _promptResultStreamHub = promptResultStreamHub;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public Task<AppResponseBase<List<PromptSelectorOption>>> GetSelectorOptionsAsync()
        {
            return this.GetResponseAsync<List<PromptSelectorOption>>(
                async (response, logger) => await _promptItemService.GetSelectorOptionsAsync());
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public Task<AppResponseBase<PromptSelectorSearchResponse>> SearchSelectorAsync(
            int rangeId = 0, string tactic = null, string keyword = null, int pageIndex = 1, int pageSize = 10,
            string sortBy = "score")
        {
            return this.GetResponseAsync<PromptSelectorSearchResponse>(
                async (response, logger) => await _promptItemService.SearchSelectorAsync(
                    rangeId, tactic, keyword, pageIndex, pageSize, sortBy));
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public Task<AppResponseBase<List<PromptSelectorScoreItem>>> GetSelectorRangeScoresAsync(int id)
        {
            return this.GetResponseAsync<List<PromptSelectorScoreItem>>(
                async (response, logger) => await _promptItemService.GetSelectorRangeScoresAsync(id));
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public Task<AppResponseBase<PromptReferenceInfo>> GetReferenceInfoAsync(int id)
        {
            return this.GetResponseAsync<PromptReferenceInfo>(
                async (response, logger) => await GetService<IPromptReferenceService>().GetInfoAsync(id));
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public Task<AppResponseBase<PromptReferenceInfo>> ScoreReferenceAsync(PromptReferenceScoreRequest request)
        {
            return this.GetResponseAsync<PromptReferenceInfo>(
                async (response, logger) => await GetService<IPromptReferenceService>().ScoreAsync(request.Id, request.Score));
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public Task<AppResponseBase<PromptSelectorItem>> GetSelectorPromptAsync(int id)
        {
            return this.GetResponseAsync<PromptSelectorItem>(
                async (response, logger) => await _promptItemService.GetSelectorPromptAsync(id));
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptItem_AddResponse>> Add(PromptItem_AddRequest request)
        {
            return await this.GetResponseAsync<PromptItem_AddResponse>(
                async (response, logger) =>
                {
                    try
                    {
                    // addpromptItem
                    var savedPromptItem = await _promptItemService.AddPromptItemAsync(request);
                    // ?? throw new NcfExceptionBase("addfailure");

                    var promptItemResponseDto = new PromptItem_AddResponse(savedPromptItem);

                    //Legacy comment translated to English.
                    if (request.IsDraft)
                    {
                        return promptItemResponseDto;
                    }

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    List<Domain.Services.ChatMessageDto> chatHistory = null;
                    if (request.ChatHistory != null && request.ChatHistory.Count > 0)
                    {
                        chatHistory = request.ChatHistory.Select(h => new Domain.Services.ChatMessageDto
                        {
                            Role = h.Role,
                            Content = h.Content
                        }).ToList();
                    }
                    
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    ResultMode? firstResultMode = null;
                    string firstUserMessage = request.UserMessage;
                    List<Domain.Services.ChatMessageDto> firstChatHistory = chatHistory;
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
                    
                    for (var i = 0; i < request.NumsOfResults; i++)
                    {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        string currentUserMessage = null;
                        List<Domain.Services.ChatMessageDto> currentChatHistory = null;
                        
                        if (i == 0)
                        {
                            //Legacy comment translated to English.
                            currentUserMessage = request.UserMessage;
                            currentChatHistory = chatHistory;
                        }
                        else if (firstResultMode == ResultMode.Chat && !string.IsNullOrWhiteSpace(firstUserMessage))
                        {
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; //Legacy comment translated to English.
                        }
                        //Legacy comment translated to English.
                        
                        //Legacy comment translated to English.
                        PromptResultDto promptResult = await _promptResultService.SenparcGenerateResultAsync(
                            savedPromptItem,
                            currentUserMessage,
                            currentChatHistory,
                            streamCallback,
                            request.ExecutionOptions);
                        
                        //Legacy comment translated to English.
                        if (i == 0)
                        {
                            firstResultMode = promptResult.Mode;
                            firstUserMessage = currentUserMessage;
                        }
                        
                        promptItemResponseDto.PromptResultList.Add(promptResult);
                    }

                    if (!string.IsNullOrWhiteSpace(request.StreamId))
                    {
                        _promptResultStreamHub.Publish(new PromptResultStreamEvent
                        {
                            StreamId = request.StreamId,
                            EventType = "complete",
                            IsFinal = true
                        });
                    }

                    await _promptResultService.UpdateEvalScoreAsync(savedPromptItem.Id);

                    return promptItemResponseDto;
                    }catch(Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                        throw;
                    }
                }
            );
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="rangeName">rangename（must）</param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName(string rangeName)
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>, List<PromptItem_GetIdAndNameResponse>>(
                    async (response, logger) =>
                    {
                        var promptItemTreeList = await _promptItemService.GetPromptRangeTreeList(false, false);

                        List<PromptItem> promptItems = await _promptItemService
                            .GetFullListAsync(p => p.RangeName == rangeName,
                                p => p.Id,
                                Ncf.Core.Enums.OrderingType.Ascending);
                        return promptItems.Select(p => new PromptItem_GetIdAndNameResponse(p)
                        ).ToList();
                    });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="rangeId">range ID（must）</param>
        /// <returns></returns>
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName(int rangeId)
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>, List<PromptItem_GetIdAndNameResponse>>(
                    async (response, logger) =>
                    {
                        List<PromptItem> promptItems = await _promptItemService
                            .GetFullListAsync(p => p.RangeId == rangeId,
                                p => p.Id,
                                Ncf.Core.Enums.OrderingType.Ascending);
                        return promptItems.Select(p => new PromptItem_GetIdAndNameResponse(p)
                        ).ToList();
                    });
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        // [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetRangeNameListResponse>>> GetRangeNameList()
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetRangeNameListResponse>>, List<PromptItem_GetRangeNameListResponse>>(
                    async (response, logger) =>
                    {
                        List<PromptItem> promptItems = await _promptItemService
                            .GetFullListAsync(
                                p => true,
                                p => p.Id,
                                Ncf.Core.Enums.OrderingType.Ascending);

                        return promptItems.DistinctBy(p => p.RangeName)
                            .Select(p => new PromptItem_GetRangeNameListResponse
                            {
                                Id = p.Id,
                                RangeName = p.RangeName,
                            }).ToList();
                    });
        }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // /// <param name="promptItemId"></param>
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<AppResponseBase<TacticTree_GetResponse>> FindItemHistory(int promptItemId)
        // {
        //Legacy comment translated to English.
        //     return await this.GetResponseAsync<TacticTree_GetResponse>(
        //         async (resp, logger) =>
        //         {
        //             var root = await _promptItemService.GenerateVersionTree(promptItemId);
        //             return new TacticTree_GetResponse(root);
        //         });
        // }

        /// <summary>
        /// according toprimary key ID　get PromptItem allinformation
        /// </summary>
        /// <param name="id">primary key ID </param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<PromptItem_GetResponse>> Get(int id)
        {
            return await this.GetResponseAsync<PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    // getpromptItem
                    PromptItemDto promptItem = await _promptItemService.GetAsync(id);

                    //Legacy comment translated to English.
                    var resp = new PromptItem_GetResponse(promptItem);

                    //Legacy comment translated to English.
                    var resultList = await _promptResultService.GetFullListAsync(res => res.PromptItemId == promptItem.Id);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        //Legacy comment translated to English.
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<AppResponseBase<PromptItem_GetResponse>> GetByVersion(string fullVersion)
        // {
        //     return await this.GetResponseAsync<PromptItem_GetResponse>(
        //         async (response, logger) =>
        //         {
        //             SenparcAI_GetByVersionResponse promptItem = await _promptItemService.GetWithVersionAsync(fullVersion);
        //
        //             List<PromptResult> resultList = await _promptResultService.GetFullListAsync(result => result.PromptItemId == promptItem.Id);
        //
        //             var resp = new PromptItem_GetResponse(promptItem);
        //             resp.PromptResultList.AddRange(resultList);
        //
        //             return resp;
        //         });
        // }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="rangeName">rangename</param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<TacticTree_GetResponse>> GetTacticTree(string rangeName)
        {
            return await this.GetResponseAsync<TacticTree_GetResponse>(
                async (resp, logger) =>
                {
                    var allPromptItems = await _promptItemService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                    var tacticTree = await _promptItemService.GenerateTacticTreeAsync(allPromptItems, rangeName);

                    return new TacticTree_GetResponse(tacticTree);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Modify(PromptItem_ModifyRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var item = await _promptItemService.GetObjectAsync(p => p.Id == request.Id) 
                        ?? throw new Exception($"未找到 id 为 {request.Id} 的 PromptItem");
                
                //Legacy comment translated to English.
                if (!string.IsNullOrWhiteSpace(request.NickName))
                {
                    //Legacy comment translated to English.
                    var sameNameItem = await _promptItemService.GetObjectAsync(z => z.RangeId == item.RangeId && z.NickName == request.NickName);
                    if (sameNameItem != null)
                    {
                        sameNameItem.ModifyNickName(null);
                        await _promptItemService.SaveObjectAsync(sameNameItem);
                    }
                }

                if (request.NickName != null)
                {
                    //Legacy comment translated to English.
                    item.ModifyNickName(request.NickName == "" ? null : request.NickName);
                }


                if (!string.IsNullOrWhiteSpace(request.Note))
                {
                    item.ModifyNote(request.Note);
                }

                await _promptItemService.SaveObjectAsync(item);

                return "ok";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> UpdateDraftAsync(int promptItemId, PromptItemDto dto)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var item = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId && p.IsDraft == true) ??
                           throw new Exception($"未找到 ID　为{promptItemId}prompt草稿");

                item.UpdateDraft(dto);

                await _promptItemService.SaveObjectAsync(item);

                return "ok";
            });
        }

        /// <summary>
        /// according toprimary key ID　delete PromptItem allinformation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteAsync(int id)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == id) ??
                                 throw new Exception("未找到prompt");
                List<PromptItem> toDeleteItemList = await _promptItemService.GetFullListAsync(p => p.ParentTac == promptItem.Tactic);
                toDeleteItemList.Add(promptItem);


                await _promptItemService.DeleteAllAsync(toDeleteItemList);

                //Legacy comment translated to English.
                var toDeleteIdList = toDeleteItemList.Select(p => p.Id).ToList();
                await _promptResultService.BatchDeleteWithItemId(toDeleteIdList);

                return "ok";
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="promptItemId"></param>
        /// <param name="expectedResults"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptItemDto>> UpdateExpectedResults(int promptItemId, string expectedResults)
        {
            return await this.GetResponseAsync<PromptItemDto>(
                async (response, logger) =>
                    await _promptItemService.UpdateExpectedResultsAsync(promptItemId, expectedResults)
            );
        }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // /// <param name="rangeName"></param>
        // /// <param name="isAvg"></param>
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<AppResponseBase<PromptItemDto>> GetBestPromptAsync(string rangeName, bool isAvg = true)
        // {
        //     return await this.GetResponseAsync<PromptItemDto>(
        //         async (response, logger) => { return await _promptItemService.GetBestPromptAsync(rangeName, isAvg); });
        // }

        /// <summary>
        /// uploadplugininterface
        /// </summary>
        /// <param name="zipFile"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> UploadPluginsAsync(IFormFile zipFile)
        {
            return await this.GetStringResponseAsync(async (resp, logger) =>
            {
                await _promptItemService.UploadPluginsAsync(zipFile);

                return "";
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<FileContentResult> ExportPluginsAsync(PromptItem_ExportRequest request)
        {
            // ids ??= new();
            var rangePath = await _promptItemService.ExportPluginsAsync(request.RangeIds, request.Ids);

            return await BuildZipStreamAsync(rangePath);
        }


        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // /// <param name="itemVersion"></param>
        // /// <returns></returns>
        // // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<FileContentResult> ExportPluginsAsync(string itemVersion)
        // {
        //     var rangePath = await _promptItemService.ExportPluginsAsync(itemVersion);
        //     return await BuildZipStream(rangePath);
        // }

        private static async Task<FileContentResult> BuildZipStreamAsync(string dirPath)
        {
            try
            {
                //Legacy comment translated to English.
                if (!Directory.Exists(dirPath))
                {
                    throw new NcfExceptionBase($"导出目录不存在: {dirPath}");
                }

                //Legacy comment translated to English.
                var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    throw new NcfExceptionBase($"导出目录中没有找到任何文件: {dirPath}");
                }

                // rangePath
                var parentDir = Directory.GetParent(dirPath);
                if (parentDir == null)
                {
                    throw new NcfExceptionBase($"无法获取父目录: {dirPath}");
                }

                var filePath = Path.Combine(
                    parentDir.FullName,
                    $"{DateTimeOffset.Now.ToLocalTime():yyyyMMddHHmmss}_ExportedPlugins.zip");

                //Legacy comment translated to English.
                ZipFile.CreateFromDirectory(
                    dirPath,
                    filePath,
                    CompressionLevel.Fastest,
                    false);

                byte[] buffer;
                await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    buffer = new byte[fileStream.Length];
                    var byteCnt = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                }

                //Legacy comment translated to English.
                try
                {
                    // delete zip file
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    
                    //Legacy comment translated to English.
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch (Exception ex)
                {
                    //Legacy comment translated to English.
                    Console.WriteLine($"清理临时文件失败: {ex.Message}");
                }

                var res = new FileContentResult(buffer, "application/octet-stream")
                {
                    FileDownloadName = "plugins.zip"
                };

                return res;
            }
            catch (Exception ex)
            {
                //Legacy comment translated to English.
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch
                {
                    //Legacy comment translated to English.
                }

                throw new NcfExceptionBase($"导出 plugins 失败: {ex.Message}", ex);
            }
        }
    }
}
