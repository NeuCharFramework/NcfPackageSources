/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Login.cshtml.cs
    文件功能描述：Login.cshtml 相关定义与实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add image captcha for admin login (required after 2 failed attempts)

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
using Senparc.CO2NET.Cache;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [BindProperties()]
    public class LoginModel : BasePageModel/* BaseAdminPageModel*/
    {
        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "请输入用户名")]
        //public string Name { get; set; }

        //[BindProperty]
        //[FromBody]
        //[Required(ErrorMessage = "请输入密码")]
        //public string Password { get; set; }

        //绑定参数
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


        public async Task<IActionResult> OnGetAsync(string ReturnUrl)
        {
            await Task.CompletedTask;
            //是否已经登录
            //var logined = await base.CheckLoginedAsync(AdminAuthorizeAttribute.AuthenticationScheme);//判断登录

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
        //        //errorMsg = "账号或密码错误！错误代码：101。";
        //        ModelState.AddModelError(nameof(this.Password), "账号或密码错误！错误代码：101。");
        //    }
        //    else if (_userInfoService.TryLogin(this.Name, this.Password, true) == null)
        //    {
        //        //errorMsg = "账号或密码错误！错误代码：102。";
        //        ModelState.AddModelError(nameof(this.Password), "账号或密码错误！错误代码：102。");
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

        public async Task<IActionResult> OnPostLoginAsync([FromBody] LoginInDto loginInDto)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new { loginInDto.Name, loginInDto.Password });
            }

            // 移除不必要的验证，因为ValidateTenant总是返回true
            // 租户名称不是必填项，无需验证

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
                //防用户名枚举：对不存在的用户名与真实用户错误密码保持一致的失败计数/锁定/验证码行为，
                //使响应无法区分“用户不存在”与“密码错误”
                try
                {
                    return new JsonResult(await SimulateUnknownUserFailedLoginAsync(loginInDto.Name));
                }
                catch (LoginLockException ex)
                {
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                    return new JsonResult(LoginResultDto.Fail(
                        ex.Message,
                        captchaRequired: true,
                        captcha: await _captchaService.CreateAsync()));
                }
                //ModelState.AddModelError(nameof(this.Password), "账号或密码错误！");
            }

            // ===== 图形验证码门槛：连续密码错误达到阈值后，必须先通过验证码校验 =====
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
                //TODO 需要把 userInfo 获取过程封装到下面的方法中，统一处理账号锁定
                if (await _userInfoService.TryLoginAsync(userInfo, loginInDto.Password, true, tenantKey) == null)
                {
                    //ModelState.AddModelError(nameof(this.Password), "账号或密码错误！");
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：账号或密码错误！102");
                    // TryLoginAsync 内部已累加失败次数；达到阈值后要求后续登录输入验证码
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
                //其他异常，不返回错误信息
                return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
            }
        }

        /// <summary>
        /// 获取图形验证码（纯 SVG 高干扰，一次性、短时效）
        /// </summary>
        public async Task<IActionResult> OnGetCaptcha()
        {
            var captcha = await _captchaService.CreateAsync();
            // 验证码一次性且时效短，禁止浏览器/代理缓存
            HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            return Ok(new { captcha.Token, captcha.Image });
        }

        /// <summary>
        /// 查询指定用户名当前是否需要图形验证码（连续密码错误达到阈值）
        /// </summary>
        /// <remarks>
        /// 安全说明：此匿名端点统一返回 false。是否需要验证码的真实状态由登录接口（OnPostLoginAsync）
        /// 在响应中下发（captchaRequired + 验证码图像），前端据此展示；
        /// 若此处按用户名返回真实状态，攻击者可零成本探测“某用户名是否存在/当前是否处于失败计数状态”（用户名枚举）。
        /// </remarks>
        public async Task<IActionResult> OnGetCaptchaRequired(string name)
        {
            await Task.CompletedTask;
            return Ok(false);
        }

        /// <summary>
        /// 对不存在的用户名模拟一次登录失败：累加失败计数、应用与真实用户相同的锁定规则、达到阈值后要求验证码。
        /// 保证“用户不存在”与“密码错误”的响应行为一致，消除用户名枚举预言机。
        /// </summary>
        private async Task<LoginResultDto> SimulateUnknownUserFailedLoginAsync(string submittedName)
        {
            var cache = CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();
            string failedLoginKey = $"FailedLogin:{submittedName}";
            string lockTimeKey = $"LockTime:{submittedName}";
            using (var cacheLock = await cache.BeginCacheLockAsync("LoginLock-Admin", submittedName))
            {
                var lockEndTime = await cache.GetAsync<DateTime?>(lockTimeKey);
                if (lockEndTime.HasValue && lockEndTime.Value > DateTime.Now)
                {
                    throw new LoginLockException(lockEndTime.Value);
                }

                var failedCount = await cache.GetAsync<int>(failedLoginKey);
                failedCount++;
                await cache.SetAsync(failedLoginKey, failedCount, TimeSpan.FromHours(24));

                DateTime? lockTime = null;
                if (failedCount >= 20)
                {
                    lockTime = DateTime.Now.AddHours(failedCount * 2);
                }
                else if (failedCount >= 10)
                {
                    lockTime = DateTime.Now.AddHours(2);
                }
                else if (failedCount >= 7)
                {
                    lockTime = DateTime.Now.AddMinutes(30);
                }
                else if (failedCount >= 5)
                {
                    lockTime = DateTime.Now.AddMinutes(5);
                }

                if (lockTime.HasValue)
                {
                    await cache.SetAsync(lockTimeKey, lockTime.Value, TimeSpan.FromHours(24));
                    throw new LoginLockException(lockTime.Value, showWrongLoginMessage: true);
                }

                if (failedCount >= LoginCaptchaService.CaptchaRequiredFailedCount)
                {
                    return LoginResultDto.Fail(
                        _localizer["Admin.Login.Error.InvalidCredentials"],
                        captchaRequired: true,
                        captcha: await _captchaService.CreateAsync());
                }

                return LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]);
            }
        }

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

        public IActionResult OnGetCheckMultiTenant()
        {
            return Ok(SiteConfig.SenparcCoreSetting.EnableMultiTenant);
        }
    }

    public class LoginInDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        public string Tenant { get; set; }
        public string CaptchaToken { get; set; }
        public string CaptchaCode { get; set; }

        public bool ValidateTenant()
        {
            // 租户名称不是必填的，即使在多租户模式下也是可选的
            // 直接返回true表示验证总是通过
            return true;
        }
    }

    /// <summary>
    /// 登录响应中的图形验证码信息
    /// </summary>
    public class LoginCaptchaDto
    {
        public string Token { get; set; }
        public string Image { get; set; }
    }

    /// <summary>
    /// 登录接口统一响应：成功/失败 + 是否需要图形验证码 + 新的验证码（需要时由服务端下发）
    /// </summary>
    public class LoginResultDto
    {
        public bool Success { get; set; }
        public string Msg { get; set; }
        public bool CaptchaRequired { get; set; }
        public LoginCaptchaDto Captcha { get; set; }

        public static LoginResultDto Ok()
        {
            return new LoginResultDto
            {
                Success = true
            };
        }

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