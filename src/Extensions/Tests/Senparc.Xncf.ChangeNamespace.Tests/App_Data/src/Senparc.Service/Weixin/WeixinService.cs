using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Cache;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models.WeixinTemplate;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.Containers;
using System;
using System.IO;
using System.Linq;

namespace Senparc.Service
{
    /// <summary>
    /// Provides Weixin face replacement, media download, template-message,
    /// and OAuth helper operations for the sample service layer.
    /// </summary>
    public class WeixinService /*: IWeixinService*/
    {
        /// <summary>
        /// Replaces recognized Weixin face codes with image markup.
        /// </summary>
        /// <param name="content">The source content containing face codes.</param>
        /// <returns>The content with recognized codes replaced by image elements.</returns>
        public string ReplaceWeixinFace(string content)
        {
            var weixinFaceCahce = SenparcDI.GetService<WeixinFaceCache>();
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
        /// Normalizes a pipe-delimited keyword string and optionally limits its entries.
        /// </summary>
        /// <param name="keyword">The source keyword string.</param>
        /// <param name="maxKeywordCount">The maximum entries, or zero for no limit.</param>
        /// <returns>A pipe-delimited normalized value, or <see langword="null"/> for empty input.</returns>
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
        /// Downloads a Weixin media resource, retrying once with a refreshed token
        /// when the first attempt fails.
        /// </summary>
        /// <param name="serverId">The Weixin media server identifier.</param>
        /// <param name="filePath">The destination file path.</param>
        /// <returns><see langword="true"/> when the resource is saved successfully.</returns>
        /// <remarks>Download failures are logged and converted to <see langword="false"/>.</remarks>
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
                    throw new Exception("图片上传错误（01）！");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogUtility.Weixin.Debug("DownloadTemplate exception: " + ex.Message, ex);
                return false;
            }
        }



        /// <summary>Downloads a Weixin media resource to a file.</summary>
        /// <param name="serverId">The Weixin media server identifier.</param>
        /// <param name="fileName">The destination file path.</param>
        /// <param name="getNewToken">Whether the provider should refresh its access token.</param>
        /// <returns><see langword="true"/> when the response is media and the file is written.</returns>
        /// <exception cref="IOException">Thrown when the destination file cannot be created or written.</exception>
        public bool DownloadTemplate(string serverId, string fileName, bool getNewToken = false)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var senparcWeixinSetting = SenparcDI.GetService<IOptions<SenparcWeixinSetting>>().Value;
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
                    Senparc.Ncf.Log.LogUtility.Weixin.InfoFormat("下载失败：{0}。serverId：{1}", str, serverId);
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
                Senparc.Ncf.Log.LogUtility.Weixin.InfoFormat("下载成功：Path[{0}]", fileName);
            }
            return true;
        }


        /// <summary> Sends a Weixin template message using the configured application credentials.</summary>
        /// <param name="data">The template payload and template identifier.</param>
        /// <param name="openId">The recipient's Weixin open identifier.</param>
        /// <param name="url">Optional detail URL included in the message.</param>
        /// <returns>The provider response, or <see langword="null"/> when sending fails.</returns>
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
        /// <summary>Exchanges an OAuth code for a token and loads the Weixin user profile.</summary>
        /// <param name="appId">The Weixin application identifier.</param>
        /// <param name="appSecret">The Weixin application secret.</param>
        /// <param name="code">The OAuth authorization code.</param>
        /// <returns>The OAuth user profile.</returns>
        /// <exception cref="Exception">Thrown when the OAuth provider reports an error code.</exception>
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
    }
}
