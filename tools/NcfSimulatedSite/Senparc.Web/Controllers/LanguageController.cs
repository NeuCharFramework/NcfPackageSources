/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LanguageController.cs
    File description: Related implementation.

    Created by: Senparc - 20260403

    Modified by: Senparc - 20260724
    Change description: v0.34.0 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.39.8 Updated module behavior and related support.

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
        /// The canonical list is owned by NcfLocalizationOptions. Add the culture
        /// there and create the corresponding *.resx files for each module.
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
            culture = normalizedCulture;

            // Set the culture cookie (name matches CookieRequestCultureProvider.DefaultCookieName)
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    MaxAge = TimeSpan.FromDays(365),
                    IsEssential = true,      // GDPR: mark as essential so consent isn't required for functionality
                    SameSite = SameSiteMode.Lax
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
