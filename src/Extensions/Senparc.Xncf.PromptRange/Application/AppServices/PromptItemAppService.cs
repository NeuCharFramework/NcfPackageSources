using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;


namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    ///PromptItem Management AppService
    /// TODO: Permission verification required
    /// </summary>
    //[ApiAuthorize("AdminOnly")]
    public class PromptItemAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;

        /// <inheritdoc />
        public PromptItemAppService(IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptResultService promptResultService) : base(serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptResultService = promptResultService;
        }

        /// <summary>
        //The /Add method is used to add a new PromptItem and decide whether to generate the result immediately based on the IsDraft field in the request
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
                    // Add promptItem
                    var savedPromptItem = await _promptItemService.AddPromptItemAsync(request);
                    // ?? throw new NcfExceptionBase("Add failed");

                    var promptItemResponseDto = new PromptItem_AddResponse(savedPromptItem);

                    // Whether to generate results immediately
                    if (request.IsDraft)
                    {
                        return promptItemResponseDto;
                    }

                    // If it is generated immediately, it will be generated immediately according to numsOfResults
                    // Convert history format
                    List<Domain.Services.ChatMessageDto> chatHistory = null;
                    if (request.ChatHistory != null && request.ChatHistory.Count > 0)
                    {
                        chatHistory = request.ChatHistory.Select(h => new Domain.Services.ChatMessageDto
                        {
                            Role = h.Role,
                            Content = h.Content
                        }).ToList();
                    }
                    
                    // When sending continuously, if the first result is in Chat mode, subsequent results also need to remain in Chat mode.
                    // Get the pattern of the first result to keep subsequent results consistent
                    ResultMode? firstResultMode = null;
                    string firstUserMessage = request.UserMessage;
                    List<Domain.Services.ChatMessageDto> firstChatHistory = chatHistory;
                    
                    for (var i = 0; i < request.NumsOfResults; i++)
                    {
                        // If it is generated for the first time, use the parameters passed in
                        // If it is a subsequent build and the first result is in Chat mode, stay in Chat mode
                        string currentUserMessage = null;
                        List<Domain.Services.ChatMessageDto> currentChatHistory = null;
                        
                        if (i == 0)
                        {
                            // The first generation, using the parameters passed in
                            currentUserMessage = request.UserMessage;
                            currentChatHistory = chatHistory;
                        }
                        else if (firstResultMode == ResultMode.Chat && !string.IsNullOrWhiteSpace(firstUserMessage))
                        {
                            // Subsequent generation, and the first result is in Chat mode, remains in Chat mode
                            // Use the same userMessage but don't pass the history (a separate conversation each time)
                            currentUserMessage = firstUserMessage;
                            currentChatHistory = null; // When sending continuously, each session is an independent conversation, and no history records are transferred.
                        }
                        // If the first result is Single mode, subsequent Single modes are also used (currentUserMessage is null)
                        
                        // Generate results separately
                        PromptResultDto promptResult = await _promptResultService.SenparcGenerateResultAsync(savedPromptItem, currentUserMessage, currentChatHistory);
                        
                        // Pattern to record the first result
                        if (i == 0)
                        {
                            firstResultMode = promptResult.Mode;
                            firstUserMessage = currentUserMessage;
                        }
                        
                        promptItemResponseDto.PromptResultList.Add(promptResult);
                    }

                    await _promptResultService.UpdateEvalScoreAsync(savedPromptItem.Id);

                    return promptItemResponseDto;
                }
            );
        }


        /// <summary>
        /// List the ids and names of all promptItems under the shooting range name
        /// </summary>
        /// <param name="rangeName">Range name (required)</param>
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
        /// Based on the shooting range ID, query the id and name of all promptItems in it
        /// </summary>
        /// <param name="rangeId">Range ID (required)</param>
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
        /// List the RangeName of all promptItems
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
        // /// Based on the ID, find the information of all parents of the corresponding promptItem
        // /// </summary>
        // /// <param name="promptItemId"></param>
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<AppResponseBase<TacticTree_GetResponse>> FindItemHistory(int promptItemId)
        // {
        //     // Find promptItem based on promptItemId, and then get the version
        //     return await this.GetResponseAsync<TacticTree_GetResponse>(
        //         async (resp, logger) =>
        //         {
        //             var root = await _promptItemService.GenerateVersionTree(promptItemId);
        //             return new TacticTree_GetResponse(root);
        //         });
        // }

        /// <summary>
        /// Get all information of PromptItem based on primary key ID
        /// </summary>
        /// <param name="id">Primary key ID </param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<PromptItem_GetResponse>> Get(int id)
        {
            return await this.GetResponseAsync<PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    // Get promptItem
                    PromptItemDto promptItem = await _promptItemService.GetAsync(id);

                    // convert to response
                    var resp = new PromptItem_GetResponse(promptItem);

                    // Get all corresponding results
                    var resultList = await _promptResultService.GetFullListAsync(res => res.PromptItemId == promptItem.Id);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

        // /// <summary>
        // /// Get all the information of PromptItem based on the complete version number
        // /// </summary>
        // /// <param name="fullVersion">Full version number</param>
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
        /// Get version tree
        /// </summary>
        /// <param name="rangeName">Range name</param>
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
                
                // According to the fields in the request, modify the corresponding
                if (!string.IsNullOrWhiteSpace(request.NickName))
                {
                    //Delete other PromptItems with the same name
                    var sameNameItem = await _promptItemService.GetObjectAsync(z => z.RangeId == item.RangeId && z.NickName == request.NickName);
                    if (sameNameItem != null)
                    {
                        sameNameItem.ModifyNickName(null);
                        await _promptItemService.SaveObjectAsync(sameNameItem);
                    }
                }

                if (request.NickName != null)
                {
                    //Modify the current name (if it is "", clear it)
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
        /// Delete all information of PromptItem based on primary key ID
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

                // Delete all sub-tactics associated with them
                var toDeleteIdList = toDeleteItemList.Select(p => p.Id).ToList();
                await _promptResultService.BatchDeleteWithItemId(toDeleteIdList);

                return "ok";
            });
        }

        /// <summary>
        ///Set AI automatic scoring standard interface
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
        // /// Get the best promptItem in the shooting range based on the shooting range name (automatically generated)
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
        ///Upload plugin interface
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
        /// Export range as plugin
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
        // /// Export the specified version of the target as plugin
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
                // Verify directory exists
                if (!Directory.Exists(dirPath))
                {
                    throw new NcfExceptionBase($"导出目录不存在: {dirPath}");
                }

                // Verify that there is a file in the directory
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

                // Create compressed file
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

                // Clean temporary files
                try
                {
                    // Delete zip file
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    
                    // Clean temporary folder
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch (Exception ex)
                {
                    // Failure to clean up does not affect the returned results, only logs are recorded.
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
                // Make sure temporary files are also cleaned up on errors
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                throw new NcfExceptionBase($"导出 plugins 失败: {ex.Message}", ex);
            }
        }
    }
}