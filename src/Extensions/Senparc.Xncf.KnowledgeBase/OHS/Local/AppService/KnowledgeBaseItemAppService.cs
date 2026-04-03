using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.KnowledgeBase.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    public class KnowledgeBaseItemAppService : AppServiceBase
    {
        private readonly KnowledgeBaseItemService knowledgeBasesDetailService;
        public KnowledgeBaseItemAppService(IServiceProvider serviceProvider, KnowledgeBaseItemService knowledgeBasesDetailService) : base(serviceProvider)
        {
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
        }

        /// <summary>
        /// Get the associated entry list based on the knowledge base ID (used for configuration page echo: associated files, content, etc.)
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<List<KnowledgeBaseItemDto>>> GetListByKnowledgeBaseId(int knowledgeBaseId)
        {
            return await this.GetResponseAsync<List<KnowledgeBaseItemDto>>(async (response, logger) =>
            {
                var list = await knowledgeBasesDetailService.GetListByKnowledgeBaseIdAsync(knowledgeBaseId);
                return list?.ToList() ?? new List<KnowledgeBaseItemDto>();
            });
        }

        /// <summary>
        ///Create and modify
        /// </summary>
        /// <param name="request">Request to record Dto model</param>
        /// <returns></returns>
        [ApiBind("AutoMate", ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> CreateOrUpdateAsync(KnowledgeBaseItemRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
            {
                KnowledgeBaseItemDto dto = new KnowledgeBaseItemDto()
                {
                    KnowledgeBasesId = request.KnowledgeBasesId,
                    ContentType = request.ContentType,
                    Content = request.Content,
                };
                await knowledgeBasesDetailService.CreateOrUpdateAsync(dto);
                bool result = true;
                return result;
            });
        }

    }

}
