/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseItemAppService.cs
    文件功能描述：KnowledgeBaseItemAppService 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
        /// 根据知识库ID获取关联的条目列表（用于配置页回显：已关联文件、内容等）
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
        /// 创建及修改
        /// </summary>
        /// <param name="request">请求记录Dto模型</param>
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
