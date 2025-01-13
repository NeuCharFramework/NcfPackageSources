using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

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
        public void RequestPage(string url, out DateTime requestStartTime, out HttpWebResponse webResponse, out DateTime requestEndTime)
        {
            CookieContainer cookieContainer = new CookieContainer();
            HttpWebRequest webRequest = HttpWebRequest.Create(url) as HttpWebRequest;
            webRequest.Method = "GET";

            webRequest.Timeout = this._pageRequestTimeoutMillionSeconds;//设置超时    TODO:SL中使用System.Net.HttpWebRequest，而非System.Web.HttpWebRequest，无法设置Timout
            webRequest.MaximumAutomaticRedirections = 4;

            webRequest.Headers.Add(HttpRequestHeader.ContentLanguage, "zh-CN");
            webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            webRequest.KeepAlive = true;
            webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.2)";

            webRequest.Accept = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            webRequest.Method = "GET";

            webRequest.CookieContainer = cookieContainer;

            requestStartTime = DateTime.Now;//开始请求
            webResponse = (HttpWebResponse)webRequest.GetResponse();//TODO:异步请求
            requestEndTime = DateTime.Now;//结束请求
        }
    }
}
