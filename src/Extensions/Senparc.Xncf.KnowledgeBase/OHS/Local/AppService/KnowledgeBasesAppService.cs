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
        private readonly KnowledgeBasesService knowledgeBasesService;
        private readonly KnowledgeBasesDetailService knowledgeBasesDetailService;

        public KnowledgeBasesAppService(IServiceProvider serviceProvider, KnowledgeBasesService knowledgeBasesService,KnowledgeBasesDetailService knowledgeBasesDetailService) : base(serviceProvider)
        {
            this.knowledgeBasesService = knowledgeBasesService;
            this.knowledgeBasesDetailService = knowledgeBasesDetailService;
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

                KnowledgeBasesDetailDto knowledgeBasesDetailDto = new KnowledgeBasesDetailDto()
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
                //设置查询条件
                //var seh = new SenparcExpressionHelper<KnowledgeBases>();
                //seh.ValueCompare.AndAlso(true,x => x.Id.Equals(request.id));
                //var where = seh.BuildWhereExpression();
                ////TODO:封装到 Service 中
                //var lstRecords = await knowledgeBasesService.GetObjectListAsync(1,10,where,"AddTime Desc");


                var sehDetail = new SenparcExpressionHelper<KnowledgeBasesDetail>();
                sehDetail.ValueCompare.AndAlso(true, x => x.KnowledgeBasesId.Equals(request.id));
                var whereDetail = sehDetail.BuildWhereExpression();
                var lstDetails = await knowledgeBasesDetailService.GetObjectListAsync(1, 10, whereDetail, "AddTime Desc");

                //Embedding


                logger.Append($"KnowledgeBases 新增成功！");

                logger.Append($"KnowledgeBases 详情添加成功！");

                return true;
            });
        }
    }

}
