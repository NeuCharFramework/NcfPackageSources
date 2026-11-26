/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: HomeController.cs
    File description: HomeController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Senparc.Areas.User.Models.VD;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.HttpUtility;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Log;
using Senparc.Mvc;
using Senparc.Mvc.Filter;
using Senparc.Service;
using Senparc.Ncf.Utility;
using Senparc.Weixin.Entities;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.QrCode;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Senparc.Ncf.Service;
using Senparc.Ncf.SMS;

namespace Senparc.Areas.User.Controllers
{
    [MenuFilter("Home")]
    public class HomeController : BaseUserController
    {
        private readonly AccountService _accountService;
        private readonly SmsRecordService _smsRecordService;
        private readonly SenparcWeixinSetting _senparcWeixinSetting;

        /// <summary>
        /// Initializes the legacy user home controller with account, SMS, and Weixin settings.
        /// </summary>
        /// <param name="accountService">The account persistence service.</param>
        /// <param name="smsRecordService">The SMS verification service.</param>
        /// <param name="senparcWeixinSetting">The configured Weixin application settings.</param>
        public HomeController(AccountService accountService, SmsRecordService smsRecordService, IOptionsSnapshot<SenparcWeixinSetting> senparcWeixinSetting)
        {
            _accountService = accountService;
            _smsRecordService = smsRecordService;
            _senparcWeixinSetting = senparcWeixinSetting.Value;
        }
       
        /// <summary>Renders the legacy user home page.</summary>
        /// <returns>The home view result.</returns>
        public ActionResult Index()
        {
            Home_IndexVD vd = new Home_IndexVD()
            {
            };
            return View(vd);
        }

        /// <summary>Renders the account-recovery page.</summary>
        /// <returns>The recovery view result.</returns>
        public ActionResult Recover()
        {
            Home_RecoverVD vd = new Home_RecoverVD()
            {
            };
            return View(vd);
        }

        [AllowAnonymous]
        /// <summary>
        /// Builds the registration view model, including QR and SMS verification state.
        /// </summary>
        /// <returns>The registration view result.</returns>
        public ActionResult Register()
        {
            int lastSeconds = -1;
            if (Request.HttpContext.Session.GetString("LastSendSmsTime") != null)
            {
                var lastSendSmsTime = DateTime.Parse(Request.HttpContext.Session.GetString("LastSendSmsTime"));
                var secondsSpan = lastSendSmsTime.AddSeconds(SiteConfig.SMSSENDWAITSECONDS) - DateTime.Now;
                lastSeconds = secondsSpan.TotalSeconds > 0 ? (int)secondsSpan.TotalSeconds : -1;
            }

            var regGuid = Guid.NewGuid();
            Request.HttpContext.Session.SetString("RegGuid", regGuid.ToString("N"));
            var vd = new Home_RegisterVD()
            {
                LastSeconds = lastSeconds,
                SmsToken = _smsRecordService.SetSendSmsToken(),
                RegGuid = regGuid,
                QrUrl = GetRegQrCode(regGuid)
            };

            return View(vd);
        }

        [HttpPost]
        [AllowAnonymous]
        /// <summary>
        /// Validates QR/SMS registration inputs and creates an account after successful checks.
        /// </summary>
        /// <param name="vd_form">The submitted registration form.</param>
        /// <returns>A registration view, login redirect, or retry redirect.</returns>
        public ActionResult Register(Home_RegisterVD vd_form)
        {
            //Legacy comment translated to English.
            var codeRegCache = SenparcDI.GetService<QrCodeRegCache>();
            var messageItem = codeRegCache.MessageQueue.FirstOrDefault(z => z.Data.RegGuid == vd_form.RegGuid);
            if (messageItem != null)
            {
                //Legacy comment translated to English.
                var qrCodeRegData = messageItem.Data;
                var user = _accountService.GetObject(z => z.WeixinOpenId == qrCodeRegData.OpenId);
                if (user != null)
                {
                    return RedirectToAction("Login", "Home");
                }
            }
            else
            {
                //Legacy comment translated to English.
                return RedirectToAction("Register", "Home");
            }

            var userNameForbiddenWords = new[] { "@", "~", "`", "\"", "/", "<", ">", "'", "*", " ", "　" };
            this.Validator(vd_form.UserName, "用户名", "UserName", false)
                .IsFalse(z => userNameForbiddenWords.Any(z.Contains), "{0}不能包含空格及特殊字符！", true)
                .MinLength(4)
                .IsFalse(z => Regex.IsMatch(z, @"^\d+$"), "不能全是数字", true)
                .IsFalse(z => _accountService.CheckUserNameExisted(0, z), "{0}已存在！", true);

            this.Validator(vd_form.Password, "密码", "Password")
                .IsNotNull(vd_form.Password, "请填写密码！", true)
                .MinLength(6)
                .IsEqual(vd_form.RePassword, "两次密码不一致", true);
            this.Validator(vd_form.Email, "Email", "Email", false)
                .IsEmail(true)
                .IsFalse(z => _accountService.CheckEmailExisted(0, z), "{0}已存在！", true);
            this.Validator(vd_form.Phone, "手机号", "Phone", false).IsMobile(true)
                .IsFalse(z => _accountService.CheckPhoneExisted(0, z), "{0}已存在！", true);
            var phoneCheckVal = this.Validator(vd_form.PhoneCheck, "验证码", "PhoneCheck", false);
            var qrCodeCheckVal = this.Validator(vd_form.QrCodeCheck, "微信验证码", "QrCodeCheck", false);

            if (ModelState.IsValid)
            {
                if (!SiteConfig.IsDebug)
                {
                    //Legacy comment translated to English.
                    var phoneCheckCodeCache = SenparcDI.GetService<PhoneCheckCodeCache>();
                    var data = phoneCheckCodeCache.Get(vd_form.PhoneCheck);
                    phoneCheckVal.IsTrue(z => data != null && data.Data.Phone == vd_form.Phone, "{0}错误！", true);
                    qrCodeCheckVal.IsTrue(z => messageItem.Data != null && messageItem.Data.Code == vd_form.QrCodeCheck, "{0}错误！", true);
                }
            }

            if (!ModelState.IsValid)
            {
                var smsRecord = SenparcDI.GetService<SmsRecordService>();
                var smsToken = smsRecord.GetSendSmsToken();//Legacy comment translated to English.
                vd_form.SmsToken = smsToken;
                return View(vd_form);
            }
            try
            {
                _accountService.CreateAccount(vd_form.UserName.Trim(), vd_form.Email.Trim(), vd_form.Phone.Trim(), vd_form.Password.Trim(),
                     messageItem.Data.OpenId, "", "");
                codeRegCache.Remove(messageItem.Data.Key);//Legacy comment translated to English.
                SetMessager(MessageType.success, "注册成功");
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                SetMessager(MessageType.danger, "注册失败，请稍后重试");
                var smsRecord = SenparcDI.GetService<SmsRecordService>();
                var smsToken = smsRecord.GetSendSmsToken();//Legacy comment translated to English.
                vd_form.SmsToken = smsToken;
                return View(vd_form);
            }
        }

        /// <summary>Renders the registration success page.</summary>
        /// <returns>The success view result.</returns>
        public IActionResult Success()
        {
            return View();
        }

        //[AllowAnonymous]
        //public ActionResult Login(string returnUrl)
        //{
        //    if (IsLogined)
        //    {
        //        if (returnUrl.IsNullOrEmpty())
        //        {
        //            return RedirectToAction("Index", "Home");
        //        }
        //        return Redirect(returnUrl.UrlDecode());
        //    }
        //    var vd = new Home_LoginVD()
        //    {
        //        ReturnUrl = returnUrl
        //    };
        //    int.TryParse(Request.HttpContext.Session.GetString("TryLoginTimes"), out var tryLoginTimes);
        //    vd.ShowCheckCode = tryLoginTimes > SiteConfig.TryUserLoginTimes;
        //    return View(vd);
        //}

        [AllowAnonymous]
        /// <summary>
        /// Starts the legacy Weixin OAuth login redirect for the requested return URL.
        /// </summary>
        /// <param name="returnUrl">The optional local return URL.</param>
        /// <returns>A redirect to the configured OAuth authorization endpoint.</returns>
        public ActionResult Login(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(base.UserName) && base.IsLogined)
            {
                return RedirectToAction("Index");
            }

            //Legacy comment translated to English.
            var remoteDomain = "https://weixin.senparc.com"; //SiteConfig.IsDebug ? "https://localhost:44305" : "https://weixin.senparc.com";
            var loaclDomain = $"{Request.Scheme}://{Request.Host.Value}"; //SiteConfig.IsDebug ? "http://localhost:11942" : "https://scf.senparc.com";

            var callbackUrl = $"{loaclDomain}/User/Home/OAuthCallback?returnUrl={returnUrl.UrlEncode()}";

            var url = $"{remoteDomain}/OAuth2/Authorize?appId={_senparcWeixinSetting.WeixinAppId}&redirect_uri={callbackUrl.UrlEncode()}&response_type=code&state=NCF&scope=snsapi_base#senparc_redirect";
            return Redirect(url);
        }

        /// <summary>
        /// Handles the Weixin OAuth callback, exchanges the authorization code,
        /// and redirects the user to the requested local destination.
        /// </summary>
        /// <param name="code">The OAuth authorization code.</param>
        /// <param name="state">The anti-forgery state value expected by the legacy flow.</param>
        /// <param name="returnUrl">The optional local return URL.</param>
        /// <param name="weixinId">The configured Weixin account identifier.</param>
        /// <returns>A redirect or explanatory content when authorization fails.</returns>
        [AllowAnonymous]
        public ActionResult OAuthCallback(string code, string state, string returnUrl, int weixinId = 18635)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Content("您拒绝了授权，请返回。");
            }

            if (state != "NCF")
            {
                return Content("验证失败！请从正规途径进入！");
            }

            try
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (s, certificate, chain, sslPolicyErrors) => true;

                var url = "https://weixin.senparc.com/APIs/User_Info";

                var userInfoJson = RequestUtility.HttpPost(url,
                    formData: new Dictionary<string, string>
                    {
                        {"code", code},
                        {"appId", _senparcWeixinSetting.WeixinAppId},
                        {"appSecret", _senparcWeixinSetting.WeixinAppSecret}
                    }
                );
                LogUtility.Account.Debug($"UserInfoJson:{userInfoJson}");
                var userInfo = JsonConvert.DeserializeObject<UserInfoJson>(userInfoJson);

                if (userInfo == null || userInfo.openid == null)
                {
                    //Legacy comment translated to English.
                    var errorResult = JsonConvert.DeserializeObject<WxJsonResult>(userInfoJson);
                    return RenderError($"错误：{errorResult.errcode.ToString()}");
                }

                var account = _accountService.CreateOrUpdateBySenparcWeixin(userInfo);

                _accountService.Login(account.UserName, true, null, true);

                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    return RedirectToAction(nameof(Index), "Home", new { area = "User" });
                }
                //Legacy comment translated to English.
                return Redirect(returnUrl);
            }
            catch (ErrorJsonResultException ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        /// <summary>
        /// Processes the legacy login form and redirects authenticated users to
        /// the requested local destination.
        /// </summary>
        /// <param name="vd">The submitted login model.</param>
        /// <returns>The login view on failure or a redirect on success.</returns>
        public ActionResult Login(Home_LoginVD vd)
        {
            if (IsLogined)
            {
                return RedirectToAction("Index", "Home");
            }

            this.Validator(vd.UserName, "用户名或Email", "UserName", false);
            this.Validator(vd.Password, "密码", "Password").IsNotNull(vd.Password, "请填写密码！", true);

            //string errorMsg = null;
            //if (ModelState.IsValid && Session["TryLoginTimes"] != null)
            //{
            //Legacy comment translated to English.
            //    var tryLoginTimes = (int)Session["TryLoginTimes"];
            //    if (SiteConfig.TryUserLoginTimes <= tryLoginTimes)
            //    {
            //Legacy comment translated to English.
            //                               .ValidateCheckCode(CheckCodeKind.Login, true);
            //        if (valCheckCode == null)
            //        {
            //Legacy comment translated to English.
            //        }
            //    }
            //}

            if (ModelState.IsValid)
            {
                var user = _accountService.TryLogin(vd.UserName, vd.Password, false, true);
                this.Validator(user, "账号", "UserName")
                    .IsNotNull(user, "账号或密码错误！", true);
            }

            if (!ModelState.IsValid)
            {
                int.TryParse(Request.HttpContext.Session.GetString("TryLoginTimes"), out var tryLoginTimes);

                vd.ShowCheckCode = tryLoginTimes >= SiteConfig.TryUserLoginTimes;

                Request.HttpContext.Session.SetString("TryLoginTimes", (tryLoginTimes + 1).ToString());
                return View(vd);
            }
            Request.HttpContext.Session.Remove("TryLoginTimes");
            if (vd.ReturnUrl.IsNullOrEmpty())
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(vd.ReturnUrl.UrlDecode());
        }

        /// <summary>
        /// Sends an SMS verification code when the phone number is not already registered
        /// and the submitted token passes the legacy validation flow.
        /// </summary>
        /// <param name="phone">The phone number receiving the code.</param>
        /// <param name="token">The anti-forgery or captcha token supplied by the client.</param>
        /// <returns>A JSON success or failure envelope.</returns>
        [AllowAnonymous]
        public ActionResult SendSms(string phone, string token)
        {
            //Legacy comment translated to English.
            bool exist = _accountService.CheckPhoneExisted(0, phone);
            if (exist)
            {
                return RenderJsonSuccessResult(false, new { Message = "手机号码已存在！" });
            }
            var session = Request.HttpContext.Session;
            //Legacy comment translated to English.
            if (session.GetString("LastSendSmsTime") != null)
            {
                var lastSeconds = 60 - (DateTime.Now - DateTime.Parse(session.GetString("LastSendSmsTime"))).TotalSeconds;
                if (lastSeconds > 0)
                {
                    return RenderJsonSuccessResult(false, new { Message = $"短信已发送，请查收，或在{(int)lastSeconds}秒后重试！" });
                }
            }

            var smsResult = _smsRecordService.SendPhoneCheck(Request.HttpContext.Connection.RemoteIpAddress.ToString(), phone, token);
            LogUtility.SmsLogger.InfoFormat("发送验证短信，号码：{0}，IP：{1}，Token：{2}", phone, Request.HttpContext.Connection.RemoteIpAddress.ToString(), _smsRecordService.GetSendSmsToken());

            if (smsResult == SmsResult.成功)
            {
                session.SetString("LastSendSmsTime", DateTime.Now.ToString());
                return RenderJsonSuccessResult(true, new { Message = "短信发送成功，请查收短信！", LastSendSmsSeconds = SiteConfig.SMSSENDWAITSECONDS });
            }
            return RenderJsonSuccessResult(false, new { Message = "发送失败：" + smsResult });
        }

        [AllowAnonymous]
        /// <summary>
        /// Creates the registration QR-code URL associated with a registration GUID.
        /// </summary>
        /// <param name="guid">The registration correlation identifier.</param>
        /// <returns>The generated QR-code URL.</returns>
        private string GetRegQrCode(Guid guid)
        {
            var sceneId = int.Parse(DateTime.Now.ToString("MMddHHmmss"));
            var senparcWeixinConfig = SenparcDI.GetService<IOptions<SenparcWeixinSetting>>().Value;
            var qrCodeRegCache = SenparcDI.GetService<QrCodeRegCache>();

            while (qrCodeRegCache.MessageCollection.ContainsKey(sceneId.ToString()))
            {
                sceneId++;
            }
            CreateQrCodeResult qrCodeResult = QrCodeApi.Create(senparcWeixinConfig.WeixinAppId, 600, sceneId,
                QrCode_ActionName.QR_SCENE, "reg_code");
            var qrCodeRegData = new QrCodeRegData(sceneId, qrCodeResult.expire_seconds, qrCodeResult.ticket, guid, QrCodeRegDataType.Reg);
            qrCodeRegCache.Insert(qrCodeRegData, qrCodeRegData.Key);
            return qrCodeResult.url;
        }

        /// <summary>Clears the legacy user session and redirects to the login page.</summary>
        /// <returns>A login redirect result.</returns>
        public ActionResult Logout()
        {
            _accountService.Logout();
            return Redirect("/");
            //return RedirectToAction("Index", "Home", new { Area = (string)null });
        }
    }
}
