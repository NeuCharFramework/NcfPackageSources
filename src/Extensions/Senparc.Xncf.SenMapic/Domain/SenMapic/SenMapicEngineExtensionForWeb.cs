using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public partial class SenMapicEngine
    {
        /// <summary>
        /// 请求网页，获取webResponse
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestStartTime"></param>
        /// <param name="webResponse"></param>
        /// <param name="requestEndTime"></param>
        public async Task<(HttpResponseMessage response,DateTime requestStartTime,DateTime requestEndTime)> RequestPage(string url)
        {
           var requestStartTime = DateTime.Now;//开始请求
            var cookieContainer = new CookieContainer();
           var webResponse = await Senparc.CO2NET.HttpUtility.RequestUtility.HttpResponseGetAsync(_serviceProvider, url, cookieContainer);
           var requestEndTime = DateTime.Now;//结束请求
           return (webResponse,requestStartTime,requestEndTime);
        }
    }
}
