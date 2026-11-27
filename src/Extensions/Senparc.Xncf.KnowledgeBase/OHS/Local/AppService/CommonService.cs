/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CommonService.cs
    File description: CommonService related implementation

    Created by: Senparc - 20260213

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

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
        /// Uploads a KnowledgeBase source file through the FileManager service.
        /// </summary>
        /// <param name="file">The multipart form file supplied by the administrator.</param>
        /// <returns>
        /// A response envelope containing the persisted file identifier as text.
        /// </returns>
        /// <remarks>
        /// File-service failures are converted into a failed application response and
        /// receive the user-facing message configured by this service.
        /// </remarks>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> UploadAsync(IFormFile file)
        {
            return await this.GetResponseAsync<AppResponseBase<string>, string>(async (response, logger) =>
            {
                var result = await ncfFileService.UploadFileAsync(file, NcfFileResourceScope.KnowledgeBase);
                return result.Id.ToString();
            }, exceptionHandler: (_, response, _) =>
            {
                response.ErrorMessage = "资料上传失败，请稍后重试。";
            });
        }

    }

}
