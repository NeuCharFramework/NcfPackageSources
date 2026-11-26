/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseItemAppService.cs
    File description: KnowledgeBaseItemAppService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

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

        /// <summary>
        /// Initializes knowledge-base item query and content synchronization operations.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="knowledgeBasesDetailService">The chunk/item service.</param>
        /// <param name="knowledgeBaseService">The knowledge-base domain service.</param>
        public KnowledgeBaseItemAppService(
            IServiceProvider serviceProvider,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            KnowledgeBaseService knowledgeBaseService) : base(serviceProvider)
        {
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
            this.knowledgeBaseService = knowledgeBaseService;
        }

        /// <summary>
        /// Returns all stored chunks for a knowledge base ordered by chunk index and creation time.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <returns>A response containing mapped chunk DTOs.</returns>
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
        /// Returns the configured source-file entries and inline content for a knowledge base.
        /// </summary>
        /// <param name="knowledgeBaseId">The knowledge-base identifier.</param>
        /// <returns>A response containing source configuration DTOs.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the knowledge base does not exist.</exception>
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
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "知识库资料加载失败，请稍后重试。";
            });
        }

        /// <summary>
        /// Replaces manual text content for a knowledge base through the compatibility item endpoint.
        /// </summary>
        /// <param name="request">The knowledge-base identifier, content type, and text.</param>
        /// <returns>A response indicating whether synchronization completed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the request contains file content instead of manual text.</exception>
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
