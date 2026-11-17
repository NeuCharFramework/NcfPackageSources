/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: RecallTestAppService.cs
    File description: RecallTestAppService related implementation


    Created by: Senparc - 20260225

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

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
using Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Response;
using Senparc.Xncf.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using plRequest = Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class RecallTestAppService : AppServiceBase
    {
        private readonly KnowledgeBaseService knowledgeBaseService;

        public RecallTestAppService(IServiceProvider serviceProvider,
            Domain.Services.KnowledgeBaseService knowledgeBaseService) : base(serviceProvider)
        {
            this.knowledgeBaseService = knowledgeBaseService;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<RecallTestResponse>>> RecallTest(plRequest.RecallTestRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<List<RecallTestResponse>>, List<RecallTestResponse>>(async (response, logger) =>
            {
                if (request == null || request.Id <= 0)
                {
                    throw new ArgumentException("请选择有效的知识库。", nameof(request));
                }

                request.Content = request.Content?.Trim();
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    throw new ArgumentException("请输入要测试的问题。", nameof(request));
                }

                try
                {
                    var topK = request.TopK <= 0 ? 5 : Math.Min(20, Math.Max(1, request.TopK));
                    var options = string.IsNullOrWhiteSpace(request.RerankMode)
                        ? null
                        : new RecallOptions
                        {
                            RerankMode = request.RerankMode,
                            CandidateCount = request.CandidateCount > 0 ? request.CandidateCount : (int?)null
                        };
                    //Legacy comment translated to English.
                    logger.Append($"开始召回测试：知识库 #{request.Id}，TopK={topK}，重排={options?.RerankMode ?? "跟随知识库配置"}。");
                    var result = await knowledgeBaseService.RecallTestAsync(request.Id, request.Content, topK, options);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.Append("召回测试失败。");
                    throw;
                }
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "召回测试失败，请检查配置后重试。";
            });
        }
    }

}
