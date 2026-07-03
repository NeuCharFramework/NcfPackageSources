/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CommonService.cs
    文件功能描述：CommonService 相关实现
    
    
    创建标识：Senparc - 20260213
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
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
                var result = await ncfFileService.UploadFileAsync(file);
                return result.Id.ToString();
            });
        }

    }

}
