/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseItemAppService.cs
    文件功能描述：KnowledgeBaseItemAppService 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

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
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.Services;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class KnowledgeBaseItemAppService : AppServiceBase
    {
        private readonly KnowledgeBaseItemService knowledgeBasesDetailService;
        private readonly KnowledgeBaseService knowledgeBaseService;

        public KnowledgeBaseItemAppService(
            IServiceProvider serviceProvider,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            KnowledgeBaseService knowledgeBaseService) : base(serviceProvider)
        {
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
            this.knowledgeBaseService = knowledgeBaseService;
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
        /// 配置页回显专用：文本取知识库原文，文件只返回首切片，避免把整个知识库切片传到浏览器。
        /// </summary>
        [ApiBind]
        public async Task<AppResponseBase<List<KnowledgeBaseItemDto>>> GetConfigurationByKnowledgeBaseId(int knowledgeBaseId)
        {
            return await this.GetResponseAsync<List<KnowledgeBaseItemDto>>(async (response, logger) =>
            {
                var knowledgeBase = await knowledgeBaseService.GetObjectAsync(z => z.Id == knowledgeBaseId)
                    ?? throw new InvalidOperationException($"知识库不存在：{knowledgeBaseId}");
                var list = await knowledgeBasesDetailService.GetFullListAsync(z =>
                    z.KnowledgeBasesId == knowledgeBaseId
                    && z.NcfFileId != null
                    && z.ChunkIndex == 0);
                var result = list
                    .OrderBy(z => z.NcfFileId)
                    .Select(z => knowledgeBasesDetailService.Mapping<KnowledgeBaseItemDto>(z))
                    .ToList();
                if (!string.IsNullOrWhiteSpace(knowledgeBase.Content))
                {
                    result.Add(new KnowledgeBaseItemDto(
                        0,
                        knowledgeBase.Id,
                        ContentType.Text,
                        knowledgeBase.Content));
                }
                return result;
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
                if (request.ContentType != ContentType.Text)
                {
                    throw new InvalidOperationException("此兼容接口仅允许同步手工文本；文件必须通过 FileManager 关联接口导入。");
                }

                await knowledgeBaseService.SyncInlineContentToKnowledgeBaseAsync(
                    request.KnowledgeBasesId,
                    request.Content);
                return true;
            });
        }

    }

}
