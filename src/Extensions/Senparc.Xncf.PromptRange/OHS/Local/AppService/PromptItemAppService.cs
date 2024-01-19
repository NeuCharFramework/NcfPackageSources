using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using ContentType = Azure.Core.ContentType;


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
            return await this.GetResponseAsync<AppResponseBase<PromptItem_AddResponse>, PromptItem_AddResponse>(
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
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName([NotNull] string rangeName)
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>, List<PromptItem_GetIdAndNameResponse>>(
                    async (response, logger) =>
                    {
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
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName([NotNull] int rangeId)
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
        //     return await this.GetResponseAsync<AppResponseBase<TacticTree_GetResponse>, TacticTree_GetResponse>(
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
            return await this.GetResponseAsync<AppResponseBase<PromptItem_GetResponse>, PromptItem_GetResponse>(
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
        //     return await this.GetResponseAsync<AppResponseBase<PromptItem_GetResponse>, PromptItem_GetResponse>(
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
        public async Task<AppResponseBase<TacticTree_GetResponse>> GetTacticTree([NotNull] string rangeName)
        {
            return await this.GetResponseAsync<AppResponseBase<TacticTree_GetResponse>, TacticTree_GetResponse>(
                async (resp, logger) =>
                {
                    var tacticTree = await _promptItemService.GenerateTacticTreeAsync(rangeName);

                    return new TacticTree_GetResponse(tacticTree);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Modify(PromptItem_ModifyRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var item = await _promptItemService.GetObjectAsync(p => p.Id == request.Id) ??
                           throw new Exception($"未找到id为{request.Id}的prompt");
                // 根据 request 中的字段，对应修改
                if (!string.IsNullOrWhiteSpace(request.NickName))
                {
                    item.ModifyNickName(request.NickName);
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
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
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
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
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
            return await this.GetResponseAsync<AppResponseBase<PromptItemDto>, PromptItemDto>(
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
        //     return await this.GetResponseAsync<AppResponseBase<PromptItemDto>, PromptItemDto>(
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
            return await this.GetResponseAsync<StringAppResponse, string>(async (resp, logger) =>
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
        
        private static async Task<FileContentResult> BuildZipStreamAsync(string dirPath)
        {
            // rangePath
            var filePath = Path.Combine(
                Directory.GetParent(dirPath)!.FullName,
                $"{DateTimeOffset.Now.ToLocalTime():yyyyMMddHHmmss}_ExportedPlugins.zip");
        
            ZipFile.CreateFromDirectory(
                dirPath,
                filePath);
        
            byte[] buffer;
            await using var fileStream = new FileStream(filePath, FileMode.Open);
            {
                buffer = new byte[fileStream.Length];
                var byteCnt = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            }
        
            // 清理临时文件夹
            Directory.Delete(dirPath, true);
        
            var res = new FileContentResult(buffer, "application/octet-stream")
            {
                FileDownloadName = "plugins.zip"
            };
        
            return res;
        }
    }
}