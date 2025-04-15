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
    public class KnowledgeBasesAppService : AppServiceBase
    {
        private readonly KnowledgeBasesService knowledgeBasesService;
        public KnowledgeBasesAppService(IServiceProvider serviceProvider, KnowledgeBasesService knowledgeBasesService) : base(serviceProvider)
        {
            this.knowledgeBasesService = knowledgeBasesService;
        }

        /// <summary>
        /// 创建及修改
        /// </summary>
        /// <param name="request">请求记录Dto模型</param>
        /// <returns></returns>
        [ApiBind("AutoMate", ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> CreateOrUpdateAsync(KnowledgeBasesRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
            {
                KnowledgeBasesDto dto = new KnowledgeBasesDto()
                {
                    EmbeddingModelId = request.EmbeddingModelId,
                    VectorDBId = request.VectorDBId,
                    ChatModelId = request.ChatModelId,
                    Name = request.Name,
                };
                await knowledgeBasesService.CreateOrUpdateAsync(dto);
                bool result = true;
                return result;
            });
        }

    }

}
