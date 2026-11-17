/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: KnowledgeBaseAppService.cs
    File description: KnowledgeBaseAppService related implementation
    
    
    Created by: Senparc - 20260704
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

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
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Ncf.Utility;
using plRequest = Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class KnowledgeBaseAppService : AppServiceBase
    {
        private readonly KnowledgeBaseService knowledgeBasesService;
        private readonly KnowledgeBaseItemService knowledgeBasesDetailService;
        private readonly Domain.Services.KnowledgeBaseService knowledgeBaseService;

        public KnowledgeBaseAppService(IServiceProvider serviceProvider,
            KnowledgeBaseService knowledgeBasesService,
            KnowledgeBaseItemService knowledgeBasesDetailService,
            Domain.Services.KnowledgeBaseService knowledgeBaseService) : base(serviceProvider)
        {
            this.knowledgeBasesService = knowledgeBasesService;
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
            this.knowledgeBaseService = knowledgeBaseService;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request">requestrecordDtomodel</param>
        /// <returns></returns>
        [ApiBind("AutoMate", ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> CreateOrUpdateAsync(KnowledgeBaseRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
            {
                KnowledgeBase_InsertDto dto = new KnowledgeBase_InsertDto()
                {
                    Id = request.Id,
                    EmbeddingModelId = request.EmbeddingModelId,
                    VectorDBId = request.VectorDBId,
                    ChatModelId = request.ChatModelId,
                    Name = request.Name,
                    Content = request.Content,
                    RetrievalConfig = request.RetrievalConfig,
                    NcfFileIds = request.NcfFileIds
                };
                await knowledgeBasesService.CreateOrUpdateAsync(dto);
                bool result = true;
                return result;
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "知识库保存失败，请稍后重试。";
            });
        }

        /// <summary>
        /// createorsettings KnowledgeBaseDetail
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup information></param>
        /// <param name="memberAgentTemplateIds">members AgentTemplate ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> SetKnowledgeBaseDetail(KnowledgeBaseItemRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                if (request.ContentType != ContentType.Text)
                {
                    throw new InvalidOperationException("当前接口仅支持同步手工输入的文本内容。");
                }

                await knowledgeBaseService.SyncInlineContentToKnowledgeBaseAsync(
                    request.KnowledgeBasesId,
                    request.Content);
                logger.Append("知识库文本内容已同步。");

                return true;
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "知识库内容保存失败，请稍后重试。";
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> EmbeddingKnowledgeBase(plRequest.KnowledgeBasesRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<string>, string>(async (response, logger) =>
            {
                logger.Append($"开始对知识库 ID: {request.Id} 进行向量化处理...");
                System.Console.WriteLine($"开始对知识库 ID: {request.Id} 进行向量化处理...");
                try
                {
                    var result = await knowledgeBaseService.EmbeddingKnowledgeBaseAsync(request.Id);
                    logger.Append(result);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.Append("向量化处理失败。");
                    System.Console.WriteLine("知识库向量化处理失败。");
                    throw;
                }
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "向量化失败，请检查配置后重试。";
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> ImportFilesToKnowledgeBase(plRequest.ImportFilesRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                logger.Append($"开始导入文件到知识库 ID: {request.knowledgeBaseId}");
                logger.Append($"文件数量: {request.fileIds?.Count ?? 0}");
                
                try
                {
                    if (request.fileIds == null || request.fileIds.Count == 0)
                    {
                        logger.Append("警告：未选择任何文件");
                        return false;
                    }

                    var totalChunks = await knowledgeBaseService.AddFilesToKnowledgeBaseAsync(
                        request.knowledgeBaseId, 
                        request.fileIds);
                    
                    logger.Append($"成功！共生成 {totalChunks} 个文本切片");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Append("导入失败。");
                    throw;
                }
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "文件导入失败，请稍后重试。";
            });
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> SyncFilesToKnowledgeBase(plRequest.ImportFilesRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                await knowledgeBaseService.SyncFilesToKnowledgeBaseAsync(
                    request.knowledgeBaseId,
                    request.fileIds ?? new List<int>());
                logger.Append($"知识库文件关联已同步：{request.fileIds?.Distinct().Count() ?? 0} 个文件。");
                return true;
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "知识库资料关联保存失败，请稍后重试。";
            });
        }
    }

}
