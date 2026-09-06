/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CommonService.cs
    文件功能描述：CommonService 相关实现


    创建标识：Senparc - 20260213

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview6 新增知识库生命周期管理与 Agent 模板集成

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview8 完善知识库文件删除保护、召回测试与管理界面

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
using Senparc.Ncf.Utility;
using Azure.Core;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Config;
using Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Config;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class CommonService : AppServiceBase
    {
        private readonly NcfFileService ncfFileService;

        public CommonService(IServiceProvider serviceProvider, NcfFileService ncfFileService) : base(serviceProvider)
        {
            this.ncfFileService = ncfFileService;
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> UploadAsync(IFormFile file)
        {
            return await this.GetResponseAsync<AppResponseBase<string>, string>(async (response, logger) =>
            {
                // 知识库内嵌上传只能创建资料文件，绝不能借此写入可公开的站点资源区。
                var result = await ncfFileService.UploadFileAsync(file, NcfFileResourceScope.KnowledgeBase);
                return result.Id.ToString();
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "资料上传失败，请稍后重试。";
            });
        }

    }

}
