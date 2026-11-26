/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinService.cs
    File description: WeixinService related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Log;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.WeixinManager.Domain.Cache;
using Senparc.Xncf.WeixinManager.WeixinTemplate;

namespace Senparc.Xncf.WeixinManager.Domain.Services
{
    public class WeixinService /*: IWeixinService*/
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly FindApiService _findApiService;

        /// <summary>
        /// Initializes the Weixin service with dependency access and API discovery support.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve caches and settings.</param>
        /// <param name="findApiService">The Weixin API discovery service.</param>
        public WeixinService(IServiceProvider serviceProvider, FindApiService findApiService)
        {
            _serviceProvider = serviceProvider;
            _findApiService = findApiService;
        }

        /// <summary>
        /// Replaces recognized Weixin face-code tokens with HTML image elements.
        /// </summary>
        /// <param name="content">The source text containing face codes.</param>
        /// <returns>The text with recognized face codes replaced.</returns>
        public string ReplaceWeixinFace(string content)
        {
            var weixinFaceCahce = _serviceProvider.GetService<WeixinFaceCache>();
            foreach (var keyValuePair in weixinFaceCahce.Data)
            {
                var image = $"<img src=\"/Content/WeixinFace/{keyValuePair.Value}.png\" title=\"{keyValuePair.Value.ToString()}\" />";
                //Legacy comment translated to English.
                var encodedCode = keyValuePair.Key.HtmlEncode();
                if (encodedCode != keyValuePair.Key)
                {
                    content = content.Replace(keyValuePair.Key.HtmlEncode(), image);
                }
                content = content.Replace(keyValuePair.Key, image);
            }
            return content;
        }

        /// <summary>
        /// Normalizes a pipe-delimited keyword string into the standard leading/trailing-pipe format.
        /// </summary>
        /// <param name="keyword">The raw keyword string.</param>
        /// <param name="maxKeywordCount">The optional maximum number of keywords to retain.</param>
        /// <returns>The normalized keyword string, or <see langword="null"/> for blank input.</returns>
        public string GetStandardKeyword(string keyword, int maxKeywordCount = 0)
        {
            //Legacy comment translated to English.
            if (!keyword.IsNullOrEmpty())
            {
                var keywords = keyword.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (maxKeywordCount > 0)
                {
                    keywords = keywords.Take(maxKeywordCount).ToList();
                }

                return $"|{string.Join("|", keywords)}|";
            }
            return null;
        }

        /// <summary>
        /// Downloads a Weixin media resource, retrying with a refreshed token
        /// when the first attempt fails.
        /// </summary>
        /// <param name="serverId">The Weixin media server identifier.</param>
        /// <param name="filePath">The destination file path.</param>
        /// <returns><see langword="true"/> when the resource is saved successfully.</returns>
        public bool DownloadTemplate(string serverId, string filePath)
        {
            try
            {
                //createdirectory
                LogUtility.Weixin.Debug($"DownloadTemplate:path {filePath} serverId={serverId}");
                var downloadTemplateImage = DownloadTemplate(serverId, filePath, false);
                LogUtility.Weixin.Debug("DownloadTemplate:downloadTemplateImage " + downloadTemplateImage);
                if (!downloadTemplateImage)
                {
                    downloadTemplateImage = DownloadTemplate(serverId, filePath, true);
                }

                if (!downloadTemplateImage)
                {
                    throw new Exception(WeixinManagerResource.Get("Error.ImageUpload01", "Image upload failed (01)."));
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.Debug("DownloadTemplate exception: " + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Downloads a Weixin media resource to a file.
        /// </summary>
        /// <param name="serverId">The Weixin media server identifier.</param>
        /// <param name="fileName">The destination file path.</param>
        /// <param name="getNewToken">Whether to refresh the access token.</param>
        /// <returns><see langword="true"/> when media content is written.</returns>
        public bool DownloadTemplate(string serverId, string fileName, bool getNewToken = false)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var senparcWeixinSetting = _serviceProvider.GetService<IOptions<SenparcWeixinSetting>>().Value;
                MediaApi.Get(senparcWeixinSetting.WeixinAppId, serverId, ms);
                //savetofile
                ms.Position = 0;
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                //checkwhetheruploadsuccess
                byte[] topBuffer = new byte[1];
                ms.Read(topBuffer, 0, 1);
                if (topBuffer[0] == '{')
                {
                    //Legacy comment translated to English.
                    ms.Position = 0;
                    byte[] logBuffer = new byte[1024];
                    ms.Read(logBuffer, 0, logBuffer.Length);
                    string str = System.Text.Encoding.Default.GetString(logBuffer);
                    LogUtility.Weixin.InfoFormat("下载失败：{0}。serverId：{1}", str, serverId);
                    return false;
                }
                ms.Position = 0;
                //createdirectory
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    while ((bytesRead = ms.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }
                    fs.Flush();
                }
                LogUtility.Weixin.InfoFormat("下载成功：Path[{0}]", fileName);
            }
            return true;
        }


        /// <summary>
        /// Sends a Weixin template message using the configured template payload.
        /// </summary>
        /// <param name="data">The template data object.</param>
        /// <param name="openId">The recipient OpenID.</param>
        /// <param name="url">The optional template redirect URL.</param>
        /// <returns>The provider result, or <see langword="null"/> when sending fails.</returns>
        public SendTemplateMessageResult SendTemplateMessage(IWeixinTemplateBase data, string openId, string url = null)
        {
            try
            {
                var dt1 = DateTime.Now;
                //Legacy comment translated to English.
                var accessToken = AccessTokenContainer.TryGetAccessToken(
                    "wxbe855a981c34aa3f", "2d65ad33a2e8d03c79ecd7b035522227", true);
                var dt2 = DateTime.Now;

                var result = TemplateApi.SendTemplateMessage(accessToken, openId, data.TemplateId, url, data);
                var dt3 = DateTime.Now;

                LogUtility.Weixin.InfoFormat("发送模板信息【{2}】。获取AccessToken时间：{0}ms，发送时间：{1}ms",
                    (dt2 - dt1).TotalMilliseconds, (dt3 - dt2).TotalMilliseconds, data.TemplateName);
                return result;
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.ErrorFormat($"发送模板消息出错【{data.TemplateName}】：{ex.Message}", ex);
            }
            return null;
        }
        /// <summary>
        /// Exchanges an OAuth authorization code for an access token and retrieves the user profile.
        /// </summary>
        /// <param name="appId">The Weixin application identifier.</param>
        /// <param name="appSecret">The Weixin application secret.</param>
        /// <param name="code">The OAuth authorization code.</param>
        /// <returns>The OAuth user profile.</returns>
        /// <exception cref="Exception">Thrown when the Weixin OAuth API returns a failure code.</exception>
        public OAuthUserInfo GetOAuthResult(string appId, string appSecret, string code)
        {
            OAuthAccessTokenResult result = null;

            result = OAuthApi.GetAccessToken(appId, appSecret, code);

            if (result.errcode != (int)ReturnCode.请求成功)
            {
                throw new Exception(result.errcode + result.errmsg);
            }

            return OAuthApi.GetUserInfo(result.access_token, result.openid);
        }


        //[ApiBind("WeixinApi", "FindWeixinApi")]
        //public FindWeixinApiResult FindWeixinApi(PlatformType? platformType, bool? isAsync, string keyword)
        //{
        //    var result = _findApiService.FindWeixinApiResult(platformType.ToString(), isAsync, keyword);
        //    return result;
        //}
    }
}
