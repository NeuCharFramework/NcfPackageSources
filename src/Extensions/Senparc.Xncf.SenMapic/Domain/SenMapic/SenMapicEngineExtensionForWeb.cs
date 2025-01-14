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
            var headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7;charset=utf-8");
            headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            headers.Add("Accept-Encoding", "gzip, deflate, zstd");
            headers.Add("Connection", "keep-alive");
            headers.Add("Upgrade-Insecure-Requests", "1");
            headers.Add("Accept-Charset", "utf-8,gbk,gb2312,iso-8859-1");
            
            var webResponse = await Senparc.CO2NET.HttpUtility.RequestUtility.HttpResponseGetAsync(
                _serviceProvider, 
                url, 
                cookieContainer, 
                headerAddition: headers, 
                refererUrl: url,
                encoding: Encoding.UTF8
            );
            var requestEndTime = DateTime.Now;//结束请求
            return (webResponse,requestStartTime,requestEndTime);
        }
    }
}
