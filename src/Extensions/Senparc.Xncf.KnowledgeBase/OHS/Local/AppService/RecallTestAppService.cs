/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RecallTestAppService.cs
    文件功能描述：RecallTestAppService 相关实现
    
    
    创建标识：Senparc - 20260225
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request;
using Senparc.Xncf.KnowledgeBase.Domain.Services;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response;
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using plRequest = Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    public class RecallTestAppService : AppServiceBase
    {
        private readonly KnowledgeBaseService knowledgeBaseService;

        public RecallTestAppService(IServiceProvider serviceProvider,
            Domain.Services.KnowledgeBaseService knowledgeBaseService) : base(serviceProvider)
        {
            this.knowledgeBaseService = knowledgeBaseService;
        }

        /// <summary>
        /// 召回测试
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<RecallTestResponse>>> RecallTest(plRequest.RecallTestRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<List<RecallTestResponse>>, List<RecallTestResponse>>(async (response, logger) =>
            {
                logger.Append($"开始召回测试: {request.Content} ...");
                System.Console.WriteLine($"开始召回测试: {request.Content} ...");
                try
                {
                    var topK = request.TopK <= 0 ? 5 : Math.Min(20, Math.Max(1, request.TopK));
                    var result = await knowledgeBaseService.RecallTestAsync(request.Id, request.Content, topK);
                    //logger.Append(result);
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
    }

}
