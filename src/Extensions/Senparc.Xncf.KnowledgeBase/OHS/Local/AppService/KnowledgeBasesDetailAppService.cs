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
    public class KnowledgeBasesDetailAppService : AppServiceBase
    {
        private readonly KnowledgeBaseItemService knowledgeBasesDetailService;
        public KnowledgeBasesDetailAppService(IServiceProvider serviceProvider, KnowledgeBaseItemService knowledgeBasesDetailService) : base(serviceProvider)
        {
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
        }

        /// <summary>
        /// 创建及修改
        /// </summary>
        /// <param name="request">请求记录Dto模型</param>
        /// <returns></returns>
        [ApiBind("AutoMate", ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> CreateOrUpdateAsync(KnowledgeBasesDetailRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
            {
                KnowledgeBasesDetalDto dto = new KnowledgeBasesDetalDto()
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
