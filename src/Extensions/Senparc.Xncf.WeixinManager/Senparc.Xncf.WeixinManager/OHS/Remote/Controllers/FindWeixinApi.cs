/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FindWeixinApi.cs
    File description: FindWeixinApi related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.WebApi;
using Senparc.NeuChar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.OHS.Remote.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FindWeixinApiController : ControllerBase
    {
        private readonly FindApiService _findApiService;

        /// <summary>
        /// Initializes the local-only API discovery controller.
        /// </summary>
        /// <param name="findWeixinApiService">The service used to search the Weixin API catalog.</param>
        public FindWeixinApiController(FindApiService findWeixinApiService)
        {
            _findApiService = findWeixinApiService;
        }

        /// <summary>
        /// Searches the Weixin API catalog when the request originates from the local host.
        /// Remote callers receive an empty result.
        /// </summary>
        /// <param name="platformType">Optional platform category filter.</param>
        /// <param name="isAsync">Optional asynchronous API filter.</param>
        /// <param name="keyword">The API name or description search text.</param>
        /// <returns>The matching API discovery result.</returns>
        [HttpGet]
        public FindWeixinApiResult OnGetAsync(PlatformType? platformType, bool? isAsync, string keyword)
        {
            if (!Request.IsLocal())
            {
                return new FindWeixinApiResult("", null, null, new List<ApiItem>());
            }

            var category = platformType == null ? null : platformType.ToString();

            var result = _findApiService.FindWeixinApiResult(category, isAsync, keyword);
            return result;
        }
    }
}
