/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlmModelAppService.cs
    File description: LlmModelAppService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

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
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    /// <summary>
    /// LLM modelmanagement AppService
    /// TODO: permission validation is required
    /// </summary>
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class LlmModelAppService : AppServiceBase
    {
        private readonly LlModelService _llModelService;
        private readonly IMapper _mapper;

        public LlmModelAppService(
            IServiceProvider serviceProvider,
            LlModelService llModelService,
            IMapper mapper) : base(serviceProvider)
        {
            _llModelService = llModelService;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a new legacy LLM model configuration for PromptRange.
        /// </summary>
        /// <param name="request">The provider, model identifier, alias, and connection settings.</param>
        /// <returns>A response containing the persisted model DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the model configuration is invalid.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<LlModelDto>> Add(LlModel_AddRequest request)
        {
            var resp = await this.GetResponseAsync<LlModelDto>(
                async (response, logger) =>
                {
                    var llModelDto = await _llModelService.AddAsync(request);

                    return llModelDto;
                });
            return resp;
        }

        /// <summary>
        /// Updates an existing legacy LLM model configuration.
        /// </summary>
        /// <param name="request">The model identifier and replacement values.</param>
        /// <returns>A string response indicating whether the update completed.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the model does not exist or the replacement configuration is invalid.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> Modify(LlmModel_ModifyRequest request)
        {
            return await this.GetStringResponseAsync(
                async (response, logger) =>
                {
                    await _llModelService.UpdateAsync(request);

                    return "ok";
                });
        }

        /// <summary>
        /// Returns a paged legacy LLM model list filtered by model identifier.
        /// </summary>
        /// <param name="pageIndex">The page index.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="key">Optional model identifier filter.</param>
        /// <returns>A response containing mapped page items and total count.</returns>
        [ApiBind]
        public async Task<AppResponseBase<LlModel_GetPageResponse>> GetLlmModelList(int pageIndex, int pageSize, string key)
        {
            return await this.GetResponseAsync<LlModel_GetPageResponse>(
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
        /// Deletes the existing legacy LLM models identified by the supplied IDs.
        /// </summary>
        /// <param name="ids">The model identifiers to delete.</param>
        /// <returns>A string response indicating whether any model was deleted.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when persistence fails.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> BatchDelete(List<int> ids)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
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

        /// <summary>
        /// Returns model identifiers and aliases in ascending identifier order.
        /// </summary>
        /// <returns>A response containing model ID/name options.</returns>
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
