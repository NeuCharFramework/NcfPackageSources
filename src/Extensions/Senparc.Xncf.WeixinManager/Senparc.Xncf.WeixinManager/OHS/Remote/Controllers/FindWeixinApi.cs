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
