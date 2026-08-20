/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicEngineExtensionForWeb.cs
    文件功能描述：SenMapicEngineExtensionForWeb 相关实现
    
    
    创建标识：Senparc - 20250113
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260729
    修改描述：v0.3.1-preview3 限制站点地图抓取目标并安全处理重定向

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public partial class SenMapicEngine
    {
        private const int MaxRedirects = 5;

        private static readonly HttpClient SafeHttpClient = CreateSafeHttpClient();

        private static HttpClient CreateSafeHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false,
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                ConnectCallback = ConnectToPublicAddressAsync
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private static async ValueTask<Stream> ConnectToPublicAddressAsync(
            SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var publicAddresses = addresses.Where(IsPublicAddress).ToArray();
            if (publicAddresses.Length == 0)
            {
                throw new HttpRequestException("目标主机解析为非公网地址，已拒绝连接。");
            }

            Exception lastException = null;
            foreach (var address in publicAddresses)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    socket.Dispose();
                }
            }

            throw new HttpRequestException("无法连接到已通过公网地址校验的目标主机。", lastException);
        }

        /// <summary>
        /// 请求网页，获取webResponse
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestStartTime"></param>
        /// <param name="webResponse"></param>
        /// <param name="requestEndTime"></param>
        public async Task<(HttpResponseMessage response,DateTime requestStartTime,DateTime requestEndTime)> RequestPage(string url)
        {
            if (!IsAvailableUrl(url))
            {
                throw new InvalidOperationException("目标 URL 不允许访问。");
            }

            var requestStartTime = DateTime.Now;//开始请求
            var currentUri = new Uri(url);
            HttpResponseMessage webResponse = null;

            for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
            {
                if (!IsAvailableUrl(currentUri.AbsoluteUri))
                {
                    throw new InvalidOperationException("重定向目标 URL 不允许访问。");
                }

                var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7;charset=utf-8");
                request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9");
                request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
                request.Headers.TryAddWithoutValidation("Accept-Charset", "utf-8,gbk,gb2312,iso-8859-1");

                webResponse = await SafeHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!((int)webResponse.StatusCode >= 300 && (int)webResponse.StatusCode <= 399)
                    || webResponse.Headers.Location == null)
                {
                    break;
                }

                if (redirectCount == MaxRedirects)
                {
                    break;
                }

                var redirectUri = new Uri(currentUri, webResponse.Headers.Location);
                webResponse.Dispose();
                request.Dispose();
                currentUri = redirectUri;
            }

            var requestEndTime = DateTime.Now;//结束请求
            return (webResponse,requestStartTime,requestEndTime);
        }
    }
}
