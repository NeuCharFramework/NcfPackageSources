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

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    public class KnowledgeBasesAppService : AppServiceBase
    {
        private readonly KnowledgeBaseService knowledgeBasesService;
        private readonly KnowledgeBaseItemService knowledgeBasesDetailService;
        private readonly Domain.Services.KnowledgeBaseService knowledgeBaseService;

        public KnowledgeBasesAppService(IServiceProvider serviceProvider,
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

        /// <summary>
        /// 创建或设置 KnowledgeBaseDetail
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup 信息></param>
        /// <param name="memberAgentTemplateIds">成员 AgentTemplate ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> SetKnowledgeBaseDetail(KnowledgeBasesDetailRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {

                KnowledgeBasesDetalDto knowledgeBasesDetailDto = new KnowledgeBasesDetalDto()
                {
                    KnowledgeBasesId = request.KnowledgeBasesId,
                    ContentType = request.ContentType,
                    Content = request.Content
                };

                //TODO:封装到 Service 中
                await knowledgeBasesDetailService.CreateOrUpdateAsync(knowledgeBasesDetailDto);

                logger.Append($"KnowledgeBasesDetail 新增成功！");

                logger.Append($"KnowledgeBasesDetail 详情添加成功！");

                return true;
            });
        }

        /// <summary>
        /// 对KnowledgeBase进行Embedding
        /// </summary>
        /// <param name="chatGroupDto">ChatGroup 信息></param>
        /// <param name="memberAgentTemplateIds">成员 AgentTemplate ID</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> EmbeddingKnowledgeBase(plRequest.KnowledgeBasesRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                logger.Append($"开始对知识库 ID: {request.Id} 进行向量化处理...");
                System.Console.WriteLine($"开始对知识库 ID: {request.Id} 进行向量化处理...");
                try
                {
                    var result = await knowledgeBaseService.EmbeddingKnowledgeBaseAsync(request.Id);
                    logger.Append(result);
                    return true;
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
    }

}
