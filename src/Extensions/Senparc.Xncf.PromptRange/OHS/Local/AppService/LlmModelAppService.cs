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

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class LlmModelAppService : AppServiceBase
    {
        private readonly LlmModelService _llmModelService;
        private readonly IMapper _mapper;

        public LlmModelAppService(IServiceProvider serviceProvider, LlmModelService promptAddService,
            IMapper mapper) : base(serviceProvider)
        {
            _llmModelService = promptAddService;
            _mapper = mapper;
        }

        /// <summary>
        /// 添加模型
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Add(LlmModel_AddRequest request)
        {
            StringAppResponse resp = await this.GetResponseAsync<StringAppResponse, string>(
                async (response, logger) =>
                {
                    var model = await _llmModelService.AddAsync(request);

                    return "ok";
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
                    await _llmModelService.UpdateAsync(request);

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
        public async Task<AppResponseBase<LlmModel_GetPageResponse>> GetLlmModelList(int pageIndex, int pageSize, string key)
        {
            return await this.GetResponseAsync<AppResponseBase<LlmModel_GetPageResponse>, LlmModel_GetPageResponse>(
                async (response, logger) =>
                {
                    var seh = new SenparcExpressionHelper<LlmModel>();
                    seh.ValueCompare.AndAlso(!string.IsNullOrWhiteSpace(key), model => model.GetModelId().Contains(key));
                    var where = seh.BuildWhereExpression();

                    var llmModelList = await _llmModelService.GetObjectListAsync(pageIndex, pageSize, where, m => m.Id,
                        Ncf.Core.Enums.OrderingType.Descending);

                    return new LlmModel_GetPageResponse(_mapper.Map<List<LlmModel_GetPageItemResponse>>(llmModelList),
                        llmModelList.TotalCount);
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
                    var model = await _llmModelService.GetObjectAsync(n => n.Id == id);

                    if (model == null) continue;
                    
                    await _llmModelService.DeleteObjectAsync(model);
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
        public async Task<AppResponseBase<List<LlmModel_GetIdAndNameResponse>>> GetIdAndName()
        {
            return await this
                .GetResponseAsync<AppResponseBase<List<LlmModel_GetIdAndNameResponse>>,
                    List<LlmModel_GetIdAndNameResponse>>(async (response, logger) =>
                {
                    return (await _llmModelService
                            .GetFullListAsync(p => true, p => p.Id, Ncf.Core.Enums.OrderingType.Ascending))
                        .Select(model => new LlmModel_GetIdAndNameResponse
                        {
                            Id = model.Id,
                            Name = model.Name
                        }).ToList();
                });
        }
    }
}