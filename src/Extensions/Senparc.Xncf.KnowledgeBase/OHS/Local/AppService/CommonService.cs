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
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IOptionsMonitor<StaticResourceSetting> staticResourceSetting;
        private readonly NcfFileService ncfFileService;

        public CommonService(IServiceProvider serviceProvider, IWebHostEnvironment webHostEnvironment, IOptionsMonitor<StaticResourceSetting> staticResourceSetting,NcfFileService ncfFileService) : base(serviceProvider)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.staticResourceSetting = staticResourceSetting;
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
                return result.FilePath;
            });
        }

    }

}
