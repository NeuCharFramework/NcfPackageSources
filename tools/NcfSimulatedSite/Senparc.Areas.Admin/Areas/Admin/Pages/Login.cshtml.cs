/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Login.cshtml.cs
    文件功能描述：Login.cshtml 相关定义与实现


    创建标识：Senparc - 20241028

    修改标识：Senparc - 20261103
    修改描述：v0.12.15 add image captcha for admin login (required after 2 failed attempts)

    修改标识：Senparc - 20261105
    修改描述：登录安全加强：滑块验证码（含行为轨迹校验）+ 登录端点 IP 滑动窗口限流（429 + Retry-After）

    修改标识：Senparc - 20261107
    修改描述：滑块验证码改为纯 C# + SVG 自研（LoginSlideCaptchaService），移除 Lazy.SlideCaptcha.Core/SkiaSharp 第三方依赖

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.13.0 登录页接入滑块验证码，登录/验证码端点统一 IP 滑动窗口限流（含文本验证码端点）
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
using Microsoft.Extensions.Configuration;
using Senparc.CO2NET.Cache;
using System.Text.Json;
using System.Linq;


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
        private readonly LoginSlideCaptchaService _slideCaptcha;
        private readonly LoginIpThrottleService _ipThrottle;
        private readonly IConfiguration _configuration;
        public LoginModel(AdminUserInfoService userInfoService, TenantInfoService tenantInfoService,
            LoginCaptchaService captchaService, IStringLocalizer<AdminResource> localizer,
            LoginSlideCaptchaService slideCaptcha, LoginIpThrottleService ipThrottle, IConfiguration configuration)
        {
            this._userInfoService = userInfoService;
            this._tenantInfoService = tenantInfoService;
            this._captchaService = captchaService;
            this._localizer = localizer;
            this._slideCaptcha = slideCaptcha;
            this._ipThrottle = ipThrottle;
            this._configuration = configuration;
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

            // IP 滑动窗口限流：在源头掐断高频重试风暴（429 + Retry-After）
            var ipThrottle = await _ipThrottle.TryConsumeAsync(GetClientIp(), LoginIpThrottleService.ActionLogin);
            if (!ipThrottle.Allowed)
            {
                return CreateThrottledResponse(ipThrottle);
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
                        challenge: await BuildCaptchaChallengeAsync()));
                }
                //ModelState.AddModelError(nameof(this.Password), "账号或密码错误！");
            }

            // ===== 人机校验门槛：连续密码错误达到阈值后，必须先通过人机校验（滑块验证码优先，文本验证码兜底） =====
            int failedCount = await _userInfoService.GetFailedLoginCountAsync(userInfo.UserName);
            if (failedCount >= LoginCaptchaService.CaptchaRequiredFailedCount)
            {
                if (!await ValidateCaptchaChallengeAsync(loginInDto))
                {
                    SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：人机校验失败");
                    return new JsonResult(LoginResultDto.Fail(
                        _localizer["Admin.Login.Error.CaptchaInvalid"],
                        captchaRequired: true,
                        challenge: await BuildCaptchaChallengeAsync()));
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
                            challenge: await BuildCaptchaChallengeAsync()));
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
                    challenge: await BuildCaptchaChallengeAsync()));
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("登录失败", $"用户名：{loginInDto.Name}, 错误：{ex.Message}");
                //其他异常，不返回错误信息
                return new JsonResult(LoginResultDto.Fail(_localizer["Admin.Login.Error.InvalidCredentials"]));
            }
        }

        #region 登录安全加强（滑块验证码 + IP 限流）

        /// <summary>
        /// 客户端 IP（反向代理部署下优先取 X-Forwarded-For；限流仅用于提高重试成本，不作为安全边界）
        /// </summary>
        private string GetClientIp()
        {
            var forwarded = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!forwarded.IsNullOrEmpty())
            {
                var first = forwarded.Split(',')[0].Trim();
                if (!first.IsNullOrEmpty())
                {
                    return first;
                }
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 429 限流响应（带 Retry-After 头）
        /// </summary>
        private JsonResult CreateThrottledResponse(ThrottleResult throttle)
        {
            HttpContext.Response.Headers["Retry-After"] = throttle.RetryAfterSeconds.ToString();
            return new JsonResult(LoginResultDto.Fail(
                _localizer["Admin.Login.RateLimited"],
                retryAfterSeconds: throttle.RetryAfterSeconds))
            {
                StatusCode = 429
            };
        }

        /// <summary>
        /// 构建人机校验挑战：滑块验证码优先（AdminLogin:SlideCaptcha:Enabled，默认 true），
        /// 生成异常时自动退化为文本验证码，保证登录始终可用
        /// </summary>
        /// <summary>
        /// 滑块验证码开关（AdminLogin:SlideCaptcha:Enabled，默认 true）
        /// </summary>
        private bool IsSlideCaptchaEnabled()
        {
            var raw = _configuration["AdminLogin:SlideCaptcha:Enabled"];
            return !(raw != null && bool.TryParse(raw, out var value) && !value);
        }

        private async Task<LoginCaptchaChallenge> BuildCaptchaChallengeAsync()
        {
            if (IsSlideCaptchaEnabled())
            {
                try
                {
                    var data = await _slideCaptcha.CreateAsync();
                    if (data != null && !data.Token.IsNullOrEmpty())
                    {
                        return LoginCaptchaChallenge.Slide(data.Token, data.BackgroundImage, data.SliderImage);
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("滑块验证码生成失败，退化为文本验证码", ex.Message);
                }
            }
            var text = await _captchaService.CreateAsync();
            return LoginCaptchaChallenge.Text(text.Token, text.Image);
        }

        /// <summary>
        /// 人机校验：请求携带滑块轨迹时走滑块校验（位置 + 行为校验），否则按文本验证码兜底
        /// </summary>
        private async Task<bool> ValidateCaptchaChallengeAsync(LoginInDto loginInDto)
        {
            if (!loginInDto.SlideCaptchaId.IsNullOrEmpty())
            {
                try
                {
                    var track = string.IsNullOrEmpty(loginInDto.SlideTrack)
                        ? null
                        : JsonSerializer.Deserialize<LoginSlideTrack>(loginInDto.SlideTrack,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (track != null)
                    {
                        return await _slideCaptcha.ValidateAsync(loginInDto.SlideCaptchaId, track);
                    }
                }
                catch
                {
                    // 轨迹数据非法一律视为校验失败
                }
                return false;
            }
            return await _captchaService.ValidateAsync(loginInDto.CaptchaToken, loginInDto.CaptchaCode);
        }

        /// <summary>
        /// 下发新人机校验挑战（滑块优先，文本兜底）；按 IP 限流防止无限刷取
        /// </summary>
        public async Task<IActionResult> OnGetSlideCaptcha()
        {
            var throttle = await _ipThrottle.TryConsumeAsync(GetClientIp(), LoginIpThrottleService.ActionCaptcha);
            if (!throttle.Allowed)
            {
                return CreateThrottledResponse(throttle);
            }

            var challenge = await BuildCaptchaChallengeAsync();
            HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            if (challenge.Type == LoginCaptchaChallenge.TypeSlide)
            {
                return Ok(new { challenge.SlideId, backgroundImage = challenge.SlideBackground, sliderImage = challenge.SlideImage });
            }
            return Ok(new { challenge.TextToken, image = challenge.TextImage, textFallback = true });
        }

        #endregion

        /// <summary>
        /// 获取图形验证码（纯 SVG 高干扰，一次性、短时效）；按 IP 限流防止无限刷取逐张 OCR
        /// </summary>
        public async Task<IActionResult> OnGetCaptcha()
        {
            var throttle = await _ipThrottle.TryConsumeAsync(GetClientIp(), LoginIpThrottleService.ActionCaptcha);
            if (!throttle.Allowed)
            {
                return CreateThrottledResponse(throttle);
            }

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
                        challenge: await BuildCaptchaChallengeAsync());
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
        public string SlideCaptchaId { get; set; }
        public string SlideTrack { get; set; }

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
        public LoginSlideCaptchaDto SlideCaptcha { get; set; }
        public int RetryAfterSeconds { get; set; }

        public static LoginResultDto Ok()
        {
            return new LoginResultDto
            {
                Success = true
            };
        }

        public static LoginResultDto Fail(string msg, bool captchaRequired = false, LoginCaptchaChallenge challenge = null, int retryAfterSeconds = 0)
        {
            return new LoginResultDto
            {
                Success = false,
                Msg = msg,
                CaptchaRequired = captchaRequired,
                RetryAfterSeconds = retryAfterSeconds,
                Captcha = challenge != null && challenge.TextToken != null ? new LoginCaptchaDto
                {
                    Token = challenge.TextToken,
                    Image = challenge.TextImage
                } : null,
                SlideCaptcha = challenge != null && challenge.SlideId != null ? new LoginSlideCaptchaDto
                {
                    Id = challenge.SlideId,
                    BackgroundImage = challenge.SlideBackground,
                    SliderImage = challenge.SlideImage
                } : null
            };
        }
    }

    /// <summary>
    /// 登录响应携带的人机校验挑战：滑块验证码（优先）或文本验证码（兜底）
    /// </summary>
    public class LoginCaptchaChallenge
    {
        public const string TypeSlide = "slide";
        public const string TypeText = "text";

        public string Type { get; private set; }
        public string SlideId { get; private set; }
        public string SlideBackground { get; private set; }
        public string SlideImage { get; private set; }
        public string TextToken { get; private set; }
        public string TextImage { get; private set; }

        public static LoginCaptchaChallenge Slide(string id, string backgroundImage, string sliderImage)
        {
            return new LoginCaptchaChallenge
            {
                Type = TypeSlide,
                SlideId = id,
                SlideBackground = backgroundImage,
                SlideImage = sliderImage
            };
        }

        public static LoginCaptchaChallenge Text(string token, string image)
        {
            return new LoginCaptchaChallenge
            {
                Type = TypeText,
                TextToken = token,
                TextImage = image
            };
        }
    }

    /// <summary>
    /// 登录响应中的滑块验证码信息
    /// </summary>
    public class LoginSlideCaptchaDto
    {
        public string Id { get; set; }
        public string BackgroundImage { get; set; }
        public string SliderImage { get; set; }
    }
}