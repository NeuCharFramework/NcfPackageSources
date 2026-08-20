/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FindWeixinApi.cs
    文件功能描述：FindWeixinApi 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

        public FindWeixinApiController(FindApiService findWeixinApiService)
        {
            _findApiService = findWeixinApiService;
        }

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
