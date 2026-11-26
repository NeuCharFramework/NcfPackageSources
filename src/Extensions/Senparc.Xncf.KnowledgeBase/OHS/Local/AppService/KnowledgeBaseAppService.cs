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

        /// <summary>
        /// Initializes the knowledge-base management application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="knowledgeBasesService">The persistence service for knowledge-base records.</param>
        /// <param name="knowledgeBasesDetailService">The persistence service for knowledge-base chunks.</param>
        /// <param name="knowledgeBaseService">The domain service for content, file, and embedding operations.</param>
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
        /// Creates or updates a knowledge base, including its retrieval configuration and source selections.
        /// </summary>
        /// <param name="request">The knowledge-base definition, inline content, and selected FileManager IDs.</param>
        /// <returns>A response indicating whether persistence completed.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when names, model/vector configuration, or source data is invalid.</exception>
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
        /// Replaces the manual text content associated with a knowledge base.
        /// File-backed content must be synchronized through the file import endpoint.
        /// </summary>
        /// <param name="request">The knowledge-base identifier, content type, and text content.</param>
        /// <returns>A response indicating whether content synchronization completed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the request is not manual text content.</exception>
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
        /// Embeds all valid text chunks for the selected knowledge base into its configured vector store.
        /// </summary>
        /// <param name="request">The knowledge-base identifier to embed.</param>
        /// <returns>A response containing the embedding summary.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the knowledge base is missing or embedding configuration is invalid.</exception>
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
        /// Imports selected FileManager files into a knowledge base and reports the generated chunk count.
        /// </summary>
        /// <param name="request">The knowledge-base identifier and file identifiers.</param>
        /// <returns>A response indicating whether at least one file was imported.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when a file or knowledge base is invalid.</exception>
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
        /// Replaces the file associations for a knowledge base without re-importing unchanged files.
        /// </summary>
        /// <param name="request">The knowledge-base identifier and selected file identifiers.</param>
        /// <returns>A response indicating whether associations were synchronized.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the knowledge base or selected files are invalid.</exception>
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
