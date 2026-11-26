/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Login.cshtml.cs
    File description: Login.cshtml related definitions and implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261103
    Change description: v0.12.15 add image captcha for admin login (required after 2 failed attempts)

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Services;
using System;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.Tenant.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [BindProperties()]
    public class LoginModel : BasePageModel/* BaseAdminPageModel*/
    {
        //[BindProperty]
        //[FromBody]
        //Legacy comment translated to English.
        //public string Name { get; set; }

        //[BindProperty]
        //[FromBody]
        //Legacy comment translated to English.
        //public string Password { get; set; }

        //Legacy comment translated to English.
        //[BindProperty(SupportsGet = true)]
        //public string ReturnUrl { get; set; }



        private readonly AdminUserInfoService _userInfoService;
        private readonly TenantInfoService _tenantInfoService;
        private readonly LoginCaptchaService _captchaService;
        private readonly IStringLocalizer<AdminResource> _localizer;
        public LoginModel(AdminUserInfoService userInfoService, TenantInfoService tenantInfoService,
            LoginCaptchaService captchaService, IStringLocalizer<AdminResource> localizer)
        {
            this._userInfoService = userInfoService;
            this._tenantInfoService = tenantInfoService;
            this._captchaService = captchaService;
            this._localizer = localizer;
        }


        /// <summary>
        /// Handles the initial login-page request. The current implementation does not
        /// perform an automatic redirect and returns no explicit action result.
        /// </summary>
        /// <param name="ReturnUrl">An optional URL that can be used by the login view for post-login navigation.</param>
        /// <returns>The action result produced by the login page handler; currently <see langword="null"/>.</returns>
        public async Task<IActionResult> OnGetAsync(string ReturnUrl)
        {
            await Task.CompletedTask;
            //Legacy comment translated to English.
            //Legacy comment translated to English.

            //if (logined)
            //{
            //    if (ReturnUrl.IsNullOrEmpty())
            //    {
            //        return RedirectToPage("/Index");
            //    }
            //    return LocalRedirect(ReturnUrl.UrlDecode());
            //}

            return null;
        }

        //public async Task<IActionResult> OnPostAsync(/*[Required]string name,string password*/)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return null;
        //    }
        //    string errorMsg = null;

        //    var userInfo = await _userInfoService.GetUserInfo(this.Name);
        //    if (userInfo == null)
        //    {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //    }
        //    else if (_userInfoService.TryLogin(this.Name, this.Password, true) == null)
        //    {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //    }

        //    if (!errorMsg.IsNullOrEmpty() || !ModelState.IsValid)
        //    {
        //        //this.MessagerList = new List<Messager>
        //        //{
        //        //    new Messager(Senparc.Ncf.Core.Enums.MessageType.danger, errorMsg)
        //        //};
        //        return null;
        //    }

        //    if (this.ReturnUrl.IsNullOrEmpty())
        //    {
        //        return RedirectToPage("/Index");
        //    }
        //    return LocalRedirect(this.ReturnUrl.UrlDecode());
        //}

        /// <summary>
        /// Authenticates an administrator, optionally selects a tenant, enforces the failed-login
        /// captcha threshold, and returns a JSON login result. Authentication failures are converted
        /// into a generic invalid-credentials response except for account-lock responses, which
        /// include the lock message and a newly generated captcha.
        /// </summary>
        /// <param name="loginInDto">The submitted credentials, tenant selector, and captcha values.</param>
        /// <returns>A <see cref="JsonResult"/> containing a success or localized failure payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown by downstream services when required login data is null.</exception>
        public async Task<IActionResult> OnPostLoginAsync([FromBody] LoginInDto loginInDto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new { loginInDto.Name, loginInDto.Password });
            }

            //Legacy comment translated to English.
            //Legacy comment translated to English.

            AdminUserInfo userInfo = null;
            string tenantKey = loginInDto.Tenant;
            try
            {
                if (SiteConfig.SenparcCoreSetting.EnableMultiTenant && !tenantKey.IsNullOrEmpty())
                {
                    var tenantInfo = await this._tenantInfoService.GetObjectAsync(z => z.TenantKey.ToUpper() == tenantKey.ToUpper());
                    if (tenantInfo == null)
                    {
                        SenparcTrace.SendCustomLog("登录失败", $", 错误：租户名称错误：" + tenantKey);
                        return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
                    }

                    var requestTenantInfo = this._tenantInfoService.GetRequestTenantInfo(tenantInfo);
                    if (!_userInfoService.SetTenantInfo(requestTenantInfo))
                    {
                        SenparcTrace.SendCustomLog("租户配置失败", $", 错误：租户名配置错误：" + tenantKey);
                        return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
                    }

                    tenantKey = tenantInfo.TenantKey;
                }

                userInfo = await _userInfoService.GetUserInfoAsync(loginInDto.Name);
            }
            catch (Exception ex)
            {
                return new JsonResult(LoginResultDto.Fail(ex.Message));
            }

            if (userInfo == null)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：账号或密码错误！101");
                return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
                //Legacy comment translated to English.
            }

            //Legacy comment translated to English.
            int failedCount = await _userInfoService.GetFailedLoginCountAsync(userInfo.UserName);
            if (failedCount >= LoginCaptchaService.CaptchaRequiredFailedCount)
            {
                if (!await _captchaService.ValidateAsync(loginInDto.CaptchaToken, loginInDto.CaptchaCode))
                {
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：图形验证码错误");
                    return new JsonResult(LoginResultDto.Fail(
                        _localizer["Admin.Login.Error.CaptchaInvalid"],
                        captchaRequired: true,
                        captcha: await _captchaService.CreateAsync()));
                }
            }

            try
            {
                //Legacy comment translated to English.
                if (await _userInfoService.TryLoginAsync(userInfo, loginInDto.Password, true, tenantKey) == null)
                {
                    //Legacy comment translated to English.
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：账号或密码错误！102");
                    //Legacy comment translated to English.
                    int failedCountAfter = await _userInfoService.GetFailedLoginCountAsync(userInfo.UserName);
                    if (failedCountAfter >= LoginCaptchaService.CaptchaRequiredFailedCount)
                    {
                        return new JsonResult(LoginResultDto.Fail(
                            _localizer["Admin.Login.Error.InvalidCredentials"],
                            captchaRequired: true,
                            captcha: await _captchaService.CreateAsync()));
                    }
                    return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
                }

                return new JsonResult(LoginResultDto.Ok());
            }
            catch (LoginLockException ex)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                return new JsonResult(LoginResultDto.Fail(
                    ex.Message,
                    captchaRequired: true,
                    captcha: await _captchaService.CreateAsync()));
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                //Legacy comment translated to English.
                return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
            }
        }

        /// <summary>
        /// Creates a new login captcha and marks the response as non-cacheable.
        /// </summary>
        /// <returns>An <see cref="OkObjectResult"/> containing the captcha token and image.</returns>
        public async Task<IActionResult> OnGetCaptcha()
        {
            var captcha = await _captchaService.CreateAsync();
            //Legacy comment translated to English.
            HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return Ok(new { captcha.Token, captcha.Image });
        }

        /// <summary>
        /// Determines whether the supplied user has reached the failed-login threshold
        /// at which a captcha is required.
        /// </summary>
        /// <param name="name">The user name to inspect; blank input returns <see langword="false"/>.</param>
        /// <returns>An <see cref="OkObjectResult"/> containing the captcha-required flag.</returns>
        public async Task<IActionResult> OnGetCaptchaRequired(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(false);
            }

            AdminUserInfo userInfo = null;
            try
            {
                userInfo = await _userInfoService.GetUserInfoAsync(name);
            }
            catch
            {
                userInfo = null;
            }

            int failedCount = userInfo == null
                ? 0
                : await _userInfoService.GetFailedLoginCountAsync(userInfo.UserName);
            return Ok(failedCount >= LoginCaptchaService.CaptchaRequiredFailedCount);
        }

        /// <summary>
        /// Terminates the current administrator session and redirects either to the supplied
        /// local return URL or to the administrator login page.
        /// </summary>
        /// <param name="ReturnUrl">An optional local URL to use after logout.</param>
        /// <returns>A redirect action for the selected destination.</returns>
        public async Task<IActionResult> OnGetLogoutAsync(string ReturnUrl)
        {
            SenparcTrace.SendCustomLog("管理员退出登录", $"用户名：{base.UserName}");
            await _userInfoService.LogoutAsync();
            if (string.IsNullOrEmpty(ReturnUrl))
            {
                return RedirectToPage(new { area = "Admin" });
            }
            else
            {
                return LocalRedirect(ReturnUrl.UrlDecode());
            }
        }

        /// <summary>
        /// Returns whether multi-tenant behavior is enabled in the current site configuration.
        /// </summary>
        /// <returns>An <see cref="OkObjectResult"/> containing the configured Boolean value.</returns>
        public IActionResult OnGetCheckMultiTenant()
        {
            return Ok(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }
    }

    /// <summary>
    /// Carries the credentials, tenant selector, and captcha values submitted by the login form.
    /// </summary>
    public class LoginInDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        public string Tenant { get; set; }
        public string CaptchaToken { get; set; }
        public string CaptchaCode { get; set; }

        /// <summary>
        /// Validates the tenant selector according to the login DTO's current validation contract.
        /// The current contract accepts every value.
        /// </summary>
        /// <returns><see langword="true"/> for the current validation contract.</returns>
        public bool ValidateTenant()
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            return true;
        }
    }

    /// <summary>
    /// Contains the token and image returned when an administrator must complete a captcha.
    /// </summary>
    public class LoginCaptchaDto
    {
        /// <summary>
        /// Gets or sets the opaque captcha token submitted during validation.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Gets or sets the rendered captcha image payload.
        /// </summary>
        public string Image { get; set; }
    }

    /// <summary>
    /// Represents the success or failure payload returned by the login endpoint.
    /// </summary>
    public class LoginResultDto
    {
        /// <summary>
        /// Gets or sets whether authentication completed successfully.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Gets or sets the localized failure message, when authentication fails.
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// Gets or sets whether the client must display a captcha challenge.
        /// </summary>
        public bool CaptchaRequired { get; set; }
        /// <summary>
        /// Gets or sets the captcha challenge returned with a failure response.
        /// </summary>
        public LoginCaptchaDto Captcha { get; set; }

        /// <summary>
        /// Creates a successful login result without a failure message or captcha.
        /// </summary>
        /// <returns>A successful <see cref="LoginResultDto"/>.</returns>
        public static LoginResultDto Ok()
        {
            return new LoginResultDto
            {
                Success = true
            };
        }

        /// <summary>
        /// Creates a failed login result and optionally projects a captcha challenge
        /// returned by the captcha service.
        /// </summary>
        /// <param name="msg">The localized failure message.</param>
        /// <param name="captchaRequired">Whether the client must complete a captcha.</param>
        /// <param name="captcha">The optional service captcha model to project.</param>
        /// <returns>A failed <see cref="LoginResultDto"/>.</returns>
        public static LoginResultDto Fail(string msg, bool captchaRequired = false, LoginCaptchaService.CaptchaModel captcha = null)
        {
            return new LoginResultDto
            {
                Success = false,
                Msg = msg,
                CaptchaRequired = captchaRequired,
                Captcha = captcha == null ? null : new LoginCaptchaDto
                {
                    Token = captcha.Token,
                    Image = captcha.Image
                }
            };
        }
    }
}
