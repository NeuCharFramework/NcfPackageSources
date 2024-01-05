using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptItemAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;

        /// <inheritdoc />
        public PromptItemAppService(IServiceProvider serviceProvider,
            PromptRangeService promptRangeService,
            PromptItemService promptItemService,
            PromptResultService promptResultService) : base(serviceProvider)
        {
            _promptRangeService = promptRangeService;
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
                    var promptItemDto = await _promptItemService.AddPromptItemAsync(request)
                                        ?? throw new NcfExceptionBase("新增失败");

                    var promptItemResponseDto = new PromptItem_AddResponse(promptItemDto);

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
                        PromptResultDto promptResult = await _promptResultService.SenparcGenerateResultAsync(promptItemDto);
                        promptItemResponseDto.PromptResultList.Add(promptResult);
                    }

                    await _promptResultService.UpdateEvalScoreAsync(promptItemDto.Id);

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
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>,
                    List<PromptItem_GetIdAndNameResponse>>(async (response, logger) =>
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
        /// 列出所有的promptItem的RangeName
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetRangeNameListResponse>>> GetRangeNameList()
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetRangeNameListResponse>>,
                    List<PromptItem_GetRangeNameListResponse>>(async (response, logger) =>
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
                    PromptItemDto promptItem = await _promptItemService.GetAsync(id);

                    List<PromptResult> resultList = await _promptResultService.GetFullListAsync(result => result.PromptItemId == promptItem.Id);

                    var resp = new PromptItem_GetResponse(promptItem);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

        /// <summary>
        /// 根据 完整版号 获取 PromptItem 所有信息
        /// </summary>
        /// <param name="fullVersion">完整版号</param>
        /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<PromptItem_GetResponse>> GetByVersion(string fullVersion)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptItem_GetResponse>, PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    PromptItemDto promptItem = await _promptItemService.GetWithVersionAsync(fullVersion);

                    List<PromptResult> resultList = await _promptResultService.GetFullListAsync(result => result.PromptItemId == promptItem.Id);

                    var resp = new PromptItem_GetResponse(promptItem);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

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
                var result = await _promptItemService.GetObjectAsync(p => p.Id == request.Id) ??
                             throw new Exception("未找到prompt");

                result.ModifyNickName(request.NickName);
                result.ModifyNote(request.Note);

                await _promptItemService.SaveObjectAsync(result);

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
        public async Task<StringAppResponse> Del(int id)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptItemService.GetObjectAsync(p => p.Id == id) ??
                             throw new Exception("未找到prompt");

                await _promptItemService.DeleteObjectAsync(result);

                // todo 关联删除所有子战术

                await _promptResultService.BatchDeleteWithItemId(id);

                return "ok";
            });
        }

        // /// <summary>
        // /// 设置 AI 自动打分评分标准接口
        // /// </summary>
        // /// <param name="promptItemId"></param>
        // /// <param name="expectedResults"></param>
        // /// <returns></returns>
        // [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        // public async Task<AppResponseBase<PromptRangeDto>> UpdateExpectedResults(int promptRangeId, string expectedResults)
        // {
        //     return await this.GetResponseAsync<AppResponseBase<PromptItemDto>, PromptItemDto>(
        //         async (response, logger) =>
        //         {
        //             return await _promptRangeService.UpdateExpectedResultsAsync(promptRangeId, expectedResults);
        //         });
        // }
    }
}