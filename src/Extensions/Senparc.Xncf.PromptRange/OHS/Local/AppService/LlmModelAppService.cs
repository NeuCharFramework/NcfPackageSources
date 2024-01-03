using AutoMapper;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Utility;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class LlmModelAppService : AppServiceBase
    {
        private readonly LlModelService _llModelService;
        private readonly IMapper _mapper;

        public LlmModelAppService(IServiceProvider serviceProvider, LlModelService promptAddService,
            IMapper mapper) : base(serviceProvider)
        {
            _llModelService = promptAddService;
            _mapper = mapper;
        }

        /// <summary>
        /// 添加模型
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<LlModelDto>> Add(LlmModel_AddRequest request)
        {
            var resp = await this.GetResponseAsync<AppResponseBase<LlModelDto>, LlModelDto>(
                async (response, logger) =>
                {
                    var llModelDto = await _llModelService.AddAsync(request);

                    return llModelDto;
                });
            return resp;
        }

        /// <summary>
        /// 编辑模型
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> Modify(LlmModel_ModifyRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    await _llModelService.UpdateAsync(request);

                    return "ok";
                });
        }

        /// <summary>
        /// 获取model
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<LlModel_GetPageResponse>> GetLlmModelList(int pageIndex, int pageSize, string key)
        {
            return await this.GetResponseAsync<AppResponseBase<LlModel_GetPageResponse>, LlModel_GetPageResponse>(
                async (response, logger) =>
                {
                    var seh = new SenparcExpressionHelper<LlModel>();
                    seh.ValueCompare.AndAlso(!string.IsNullOrWhiteSpace(key), model => model.GetModelId().Contains(key));
                    var where = seh.BuildWhereExpression();

                    var llmModelList = await _llModelService.GetObjectListAsync(pageIndex, pageSize, where, m => m.Id,
                        Ncf.Core.Enums.OrderingType.Descending);


                    // var llmModelGetPageItemResponses = _mapper.Map<List<LlmModel_GetPageItemResponse>>(llmModelList);

                    return new LlModel_GetPageResponse(
                        llmModelList.Select(m => _mapper.Map<LlmModel_GetPageItemResponse>(m)),
                        llmModelList.TotalCount
                    );
                });
        }

        /// <summary>
        /// 模型删除
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> BatchDelete(List<int> ids)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var deletedCount = 0;

                foreach (var id in ids)
                {
                    var model = await _llModelService.GetObjectAsync(n => n.Id == id);

                    if (model == null) continue;

                    await _llModelService.DeleteObjectAsync(model);

                    deletedCount++;
                }

                if (deletedCount == 0)
                {
                    response.StateCode = 500;
                    response.ErrorMessage = "未找到任何要删除的模型！";
                    return "false";
                }

                return "ok";
            });
        }

        [ApiBind]
        public async Task<AppResponseBase<List<LlModel_GetIdAndNameResponse>>> GetIdAndName()
        {
            return await this
                .GetResponseAsync<AppResponseBase<List<LlModel_GetIdAndNameResponse>>,
                    List<LlModel_GetIdAndNameResponse>>(async (response, logger) =>
                {
                    return (await _llModelService
                            .GetFullListAsync(p => true, p => p.Id, Ncf.Core.Enums.OrderingType.Ascending))
                        .Select(model => new LlModel_GetIdAndNameResponse
                        {
                            Id = model.Id,
                            Alias = model.Alias
                        }).ToList();
                });
        }
    }
}