/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinOAuthController.cs
    File description: WeixinOAuthController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Service;
using Senparc.Service;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using System;

namespace Senparc.Areas.WX.Controllers
{
    public class WeixinOAuthController : BaseWXController
    {
        private readonly AccountService _accountService;
        private readonly WeixinService _weixinService;
        private readonly SenparcWeixinSetting _senparcWeixinSetting;

        /// <summary>
        /// Initializes the legacy Weixin OAuth controller.
        /// </summary>
        /// <param name="accountService">The account persistence service.</param>
        /// <param name="options">The configured Weixin application settings.</param>
        /// <param name="weixinService">The OAuth exchange service.</param>
        public WeixinOAuthController(AccountService accountService, IOptions<SenparcWeixinSetting> options, WeixinService weixinService)
        {
            _accountService = accountService;
            _weixinService = weixinService;
            _senparcWeixinSetting = options.Value;
        }

        /// <summary>
        /// Validates a public-account OAuth callback, creates or refreshes the account,
        /// stores the open ID in session, and redirects to the local callback URL.
        /// </summary>
        /// <param name="callbackUrl">The local redirect target.</param>
        /// <param name="code">The OAuth authorization code.</param>
        /// <param name="state">The configured anti-forgery state value.</param>
        /// <returns>A redirect or legacy error content result.</returns>
        public IActionResult MpCallback(string callbackUrl, string code, string state)
        {
            if (code == null)
            {
                return Content("验证失败！");
            }

            if (state != Config.SenparcWeixinSetting.Token)
            {
                return Content("请从合法途径进入！");
            }

            OAuthUserInfo userInfo;
            string openId;

            try
            {
                userInfo = _weixinService.GetOAuthResult(_senparcWeixinSetting.WeixinAppId, _senparcWeixinSetting.WeixinAppSecret, code);

                openId = userInfo.openid;
            }
            catch (Exception ex)
            {

                return RenderError($"公众号OAuth授权异常，原因{ex.Message}");
            }

            //handlingAccount
            var account =
                _accountService.GetObject(z => z.WeixinOpenId == openId);
            if (account == null)
            {
                try
                {
                    account = _accountService.CreateAccountByUserInfo(userInfo);
                }
                catch (Exception ex)
                {
                    return RenderError(ex.Message);
                }
            }
            else if (account.NickName.IsNullOrEmpty() && account.PicUrl.IsNullOrEmpty())
            {
                _accountService.UpdateAccountByUserInfo(userInfo, account);
            }

            HttpContext.Session.SetString("OpenId", userInfo.openid);
            //Legacy comment translated to English.
            account.ThisLoginTime = DateTime.Now;
            _accountService.SaveObject(account);
            return Redirect(callbackUrl);
        }

        /// <summary>Removes the Weixin open ID from session.</summary>
        /// <returns>A success content result.</returns>
        public IActionResult Logout()
        {
            //Session.Abandon();
            //Session.RemoveAll();
            HttpContext.Session.Remove("OpenId");
            return Content("退出成功");
        }
    }
}
