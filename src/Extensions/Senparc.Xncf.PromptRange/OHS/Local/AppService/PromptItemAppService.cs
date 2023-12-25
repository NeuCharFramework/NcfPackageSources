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
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptItemAppService : AppServiceBase
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;

        public PromptItemAppService(IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptResultService promptResultService
        ) : base(serviceProvider)
        {
            // _promptItemRepository = promptItemRepository;
            _promptItemService = promptItemService;
            _promptResultService = promptResultService;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptItem_AddResponse>> Add(PromptItem_AddRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptItem_AddResponse>, PromptItem_AddResponse>(
                async (response, logger) =>
                {
                    #region save promptItem

                    var promptItem = await _promptItemService.AddPromptItemAsync(request);
                    if (promptItem == null)
                    {
                        throw new NcfExceptionBase("新增失败");
                    }

                    #endregion


                    var promptItemResponseDto = new PromptItem_AddResponse(promptItem);

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
                        var promptResult = await _promptResultService.SenparcGenerateResultAsync(promptItem);
                        promptItemResponseDto.PromptResultList.Add(promptResult);
                    }

                    return promptItemResponseDto;
                }
            );
        }


        /// <summary>
        /// 列出所有的promptItem的id和name
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName()
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>,
                    List<PromptItem_GetIdAndNameResponse>>(async (response, logger) =>
                {
                    List<PromptItem> promptItems = await _promptItemService
                        .GetFullListAsync(
                            p => true,
                            p => p.Id,
                            Ncf.Core.Enums.OrderingType.Ascending);
                    return promptItems.Select(p => new PromptItem_GetIdAndNameResponse
                    {
                        Id = p.Id,
                        Name = p.RangeName,
                        FullVersion = p.FullVersion,
                        EvalAvgScore = p.EvalAvgScore,
                        EvalMaxScore = p.EvalMaxScore
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

        [ApiBind]
        public async Task<AppResponseBase<PromptItem_GetResponse>> Get([FromQuery] int id)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptItem_GetResponse>, PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    var promptItem = await _promptItemService.Get(id);

                    List<PromptResult> resultList = await _promptResultService.GetFullListAsync(result => result.PromptItemId == promptItem.Id);

                    var resp = new PromptItem_GetResponse(promptItem);
                    resp.PromptResultList.AddRange(resultList);

                    return resp;
                });
        }

        /// <summary>
        /// 获取版本树
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<TacticTree_GetResponse>> GetTacticTree(string rangeName)
        {
            return await this.GetResponseAsync<AppResponseBase<TacticTree_GetResponse>, TacticTree_GetResponse>(
                async (resp, logger) =>
                {
                    var tacticTree = await _promptItemService.GenerateTacticTreeAsync(rangeName);
                    
                    return new TacticTree_GetResponse(tacticTree);
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Modify(PromptItem_ModifyRequest req)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptItemService.GetObjectAsync(p => p.Id == req.Id) ??
                             throw new Exception("未找到prompt");

                result.ModifyNickName(req.NickName);

                await _promptItemService.SaveObjectAsync(result);

                return "ok";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> Del([FromQuery] int id)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptItemService.GetObjectAsync(p => p.Id == id) ??
                             throw new Exception("未找到prompt");

                await _promptItemService.DeleteObjectAsync(result);

                await _promptResultService.BatchDeleteWithItemId(id);

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
        public async Task<StringAppResponse> UpdateExpectedResults(int promptItemId, string expectedResults)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                await _promptItemService.UpdateExpectedResultsAsync(promptItemId, expectedResults);

                return "ok";
            });
        }
    }
}