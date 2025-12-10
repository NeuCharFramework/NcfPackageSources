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
        /// Add方法用于添加一个新的PromptItem，并根据请求中的IsDraft字段决定是否立即生成结果
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
                    // 新增promptItem
                    var savedPromptItem = await _promptItemService.AddPromptItemAsync(request);
                    // ?? throw new NcfExceptionBase("新增失败");

                    var promptItemResponseDto = new PromptItem_AddResponse(savedPromptItem);

                    // 是否立即生成结果
                    if (request.IsDraft)
                    {
                        return promptItemResponseDto;
                    }

                    // 如果立即生成，就根据numsOfResults立即生成
                    for (var i = 0; i < request.NumsOfResults; i++)
                    {
                        // 分别生成结果
                        // var promptResult = await _promptResultService.GenerateResultAsync(promptItem);
                        PromptResultDto promptResult = await _promptResultService.SenparcGenerateResultAsync(savedPromptItem);
                        promptItemResponseDto.PromptResultList.Add(promptResult);
                    }

                    await _promptResultService.UpdateEvalScoreAsync(savedPromptItem.Id);

                    return promptItemResponseDto;
                }
            );
        }


        /// <summary>
        /// 列出 靶场名称 下所有的promptItem的id和name
        /// </summary>
        /// <param name="rangeName">靶场名称（必须）</param>
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
        /// 根据靶场 ID, 查询其中所有的promptItem的id和name
        /// </summary>
        /// <param name="rangeId">靶场 ID（必须）</param>
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
        /// 列出所有的promptItem的RangeName
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
        // /// 根据ID，找到对应的promptItem的所有父级的信息
        // /// </summary>
        // /// <param name="promptItemId"></param>
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<AppResponseBase<TacticTree_GetResponse>> FindItemHistory(int promptItemId)
        // {
        //     // 根据promptItemId找到promptItem， 然后获取version
        //     return await this.GetResponseAsync<TacticTree_GetResponse>(
        //         async (resp, logger) =>
        //         {
        //             var root = await _promptItemService.GenerateVersionTree(promptItemId);
        //             return new TacticTree_GetResponse(root);
        //         });
        // }

        /// <summary>
        /// 根据主键 ID　获取 PromptItem 所有信息
        /// </summary>
        /// <param name="id">主键 ID </param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<PromptItem_GetResponse>> Get(int id)
        {
            return await this.GetResponseAsync<PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    // 获取promptItem
                    PromptItemDto promptItem = await _promptItemService.GetAsync(id);

                    // 转换为 response
                    var resp = new PromptItem_GetResponse(promptItem);

                    // 获取所有对应的结果
                    var resultList = await _promptResultService.GetFullListAsync(res => res.PromptItemId == promptItem.Id);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

        // /// <summary>
        // /// 根据 完整版号 获取 PromptItem 所有信息
        // /// </summary>
        // /// <param name="fullVersion">完整版号</param>
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
        /// 获取版本树
        /// </summary>
        /// <param name="rangeName">靶场名称</param>
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
                
                // 根据 request 中的字段，对应修改
                if (!string.IsNullOrWhiteSpace(request.NickName))
                {
                    //删除其他同名的 PromptItem
                    var sameNameItem = await _promptItemService.GetObjectAsync(z => z.RangeId == item.RangeId && z.NickName == request.NickName);
                    if (sameNameItem != null)
                    {
                        sameNameItem.ModifyNickName(null);
                        await _promptItemService.SaveObjectAsync(sameNameItem);
                    }
                }

                if (request.NickName != null)
                {
                    //修改当前名称（如果是 ""，则清空）
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
        /// 根据主键 ID　删除 PromptItem 所有信息
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

                // 关联删除所有子战术
                var toDeleteIdList = toDeleteItemList.Select(p => p.Id).ToList();
                await _promptResultService.BatchDeleteWithItemId(toDeleteIdList);

                return "ok";
            });
        }

        /// <summary>
        /// 设置 AI 自动打分评分标准接口
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
        // /// 根据靶场名（自动生成）获取靶场里最好的promptItem
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
        /// 上传plugin接口
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
        /// 导出靶场为 plugin
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
        // /// 导出指定版本的靶道为 plugin
        // /// </summary>
        // /// <param name="itemVersion"></param>
        // /// <returns></returns>
        // // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        // public async Task<FileContentResult> ExportPluginsAsync(string itemVersion)
        // {
        //     var rangePath = await _promptItemService.ExportPluginsAsync(itemVersion);
        //     return await BuildZipStream(rangePath);
        // }

        /// <summary>
        /// 对话测试接口 - 使用当前Prompt作为SystemMessage进行对话
        /// </summary>
        /// <param name="promptItemId">Prompt项ID</param>
        /// <param name="message">用户消息</param>
        /// <param name="messages">历史消息列表（可选）</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> ChatTest(int promptItemId, string message, List<object> messages = null)
        {
            return await this.GetResponseAsync<string>(
                async (response, logger) =>
                {
                    // TODO: 这里需要根据实际需求实现对话逻辑
                    // 1. 根据 promptItemId 获取 PromptItem
                    // 2. 使用 PromptItem 的 Content 作为 SystemMessage
                    // 3. 调用 AI 服务进行对话
                    // 4. 返回 AI 的回复
                    
                    // Hard code 示例返回
                    var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId);
                    if (promptItem == null)
                    {
                        throw new Exception($"未找到 ID 为 {promptItemId} 的 PromptItem");
                    }
                    
                    // TODO: 实现实际的对话逻辑
                    // 这里只是示例，需要根据实际的 AI 服务调用方式来实现
                    var systemMessage = promptItem.Content ?? "";
                    
                    // 模拟返回（实际应该调用 AI 服务）
                    return $"收到消息: {message}\n\n当前使用的 SystemMessage (Prompt):\n{systemMessage}\n\n[这是硬编码的返回，请根据实际需求实现对话逻辑]";
                }
            );
        }

        private static async Task<FileContentResult> BuildZipStreamAsync(string dirPath)
        {
            try
            {
                // 验证目录是否存在
                if (!Directory.Exists(dirPath))
                {
                    throw new NcfExceptionBase($"导出目录不存在: {dirPath}");
                }

                // 验证目录中是否有文件
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

                // 创建压缩文件
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

                // 清理临时文件
                try
                {
                    // 删除 zip 文件
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    
                    // 清理临时文件夹
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch (Exception ex)
                {
                    // 清理失败不影响返回结果，只记录日志
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
                // 确保在出错时也清理临时文件
                try
                {
                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }
                }
                catch
                {
                    // 忽略清理错误
                }

                throw new NcfExceptionBase($"导出 plugins 失败: {ex.Message}", ex);
            }
        }
    }
}