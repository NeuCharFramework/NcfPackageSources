/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseAppService.cs
    文件功能描述：KnowledgeBaseAppService 相关实现
    
    
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
        /// 创建及修改
        /// </summary>
        /// <param name="request">请求记录Dto模型</param>
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
                    NcfFileIds = request.NcfFileIds
                };
                await knowledgeBasesService.CreateOrUpdateAsync(dto);
                bool result = true;
                return result;
            });
        }

        /// <summary>
        /// 创建或设置 KnowledgeBaseDetail
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup 信息></param>
        /// <param name="memberAgentTemplateIds">成员 AgentTemplate ID</param>
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
            });
        }

        /// <summary>
        /// 对KnowledgeBase进行Embedding，返回向量化结果描述（供前端展示）
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
                    logger.Append($"向量化处理失败：{ex.Message}");
                    System.Console.WriteLine(ex.Message);
                    throw;
                }
            });
        }

        /// <summary>
        /// 批量导入文件到知识库
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
                    logger.Append($"导入失败：{ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// 将知识库的 FileManager 关联完整同步为指定集合；空集合表示清空。
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
            });
        }
    }

}
