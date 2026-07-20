/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LanguageController.cs
    文件功能描述：LanguageController 相关实现
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260717
    修改描述：v0.22.0 完善站点页面、语言切换与共享脚本本地化

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Utility.Helpers;

namespace Senparc.Web.Controllers
{
    /// <summary>
    /// Language/Culture switcher controller.
    /// Sets the culture cookie that ASP.NET Core's CookieRequestCultureProvider reads on every request.
    /// The cookie persists the selection in the browser for 1 year.
    ///
    /// Endpoint: GET /api/Language/Set?culture=en&amp;returnUrl=/Admin
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LanguageController : ControllerBase
    {
        /// <summary>
        /// All cultures supported by the application.
        /// When adding a new language, also add the culture code here and create the
        /// corresponding *.resx files in Resources/ for each module.
        /// </summary>
        public static IReadOnlyList<string> SupportedCultures =>
            NcfLocalizationOptions.SupportedCultures;

        /// <summary>
        /// Sets the language preference cookie and redirects to the return URL.
        /// </summary>
        /// <param name="culture">Target culture code, e.g. "en", "zh-CN", "ja"</param>
        /// <param name="returnUrl">URL to redirect to after switching. Defaults to "/"</param>
        [HttpGet("Set")]
        public IActionResult Set(string culture, string returnUrl = "/")
        {
            // Validate culture to prevent injection; fall back to default if unknown
            if (!NcfLocalizationOptions.TryNormalizeCulture(culture, out var normalizedCulture))
            {
                normalizedCulture = NcfLocalizationOptions.DefaultCulture;
            }

            // Set the culture cookie (name matches CookieRequestCultureProvider.DefaultCookieName)
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
                new CookieOptions
                {
                    MaxAge = TimeSpan.FromDays(365),
                    IsEssential = true,      // GDPR: mark as essential so consent isn't required for functionality
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = true,
                    Secure = Request.IsHttps
                }
            );

            // Validate returnUrl to prevent open-redirect attacks
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return LocalRedirect(returnUrl);
        }
    }
}
