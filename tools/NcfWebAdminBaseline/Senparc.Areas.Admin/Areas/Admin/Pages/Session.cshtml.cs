/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Session.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260705

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Senparc.Areas.Admin;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Areas.Admin.Pages
{
    [ServiceFilter(typeof(AuthenticationResultFilterAttribute))]
    [AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class SessionModel(AdminUserInfoService adminUserInfoService, IStringLocalizer<AdminResource> localizer) : BasePageModel
    {
        private readonly AdminUserInfoService _adminUserInfoService = adminUserInfoService;
        private readonly IStringLocalizer<AdminResource> _localizer = localizer;

        /// <summary>
        /// Returns the current administrator authentication type, server time, and
        /// effective cookie/JWT expiration values.
        /// </summary>
        /// <returns>An authentication status response or HTTP 401.</returns>
        public async Task<IActionResult> OnGetStatusAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var webLoginExpireMinutes = _adminUserInfoService.GetAdminWebLoginExpireMinutes();
            var jwtExpireMinutes = _adminUserInfoService.GetBackendJwtExpireMinutes();

            var cookieAuthResult = await HttpContext.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme);
            if (cookieAuthResult?.Succeeded == true && cookieAuthResult.Principal?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = cookieAuthResult.Properties?.ExpiresUtc ?? now.AddMinutes(webLoginExpireMinutes);
                return Ok(new
                {
                    authType = "cookie",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes
                });
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = _adminUserInfoService.TryGetJwtExpiresUtc(User) ?? now.AddMinutes(jwtExpireMinutes);
                return Ok(new
                {
                    authType = "jwt",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes
                });
            }

            return Unauthorized();
        }

        /// <summary>
        /// Renews the authenticated cookie or issues a refreshed backend JWT and
        /// returns the resulting expiration metadata.
        /// </summary>
        /// <returns>The refreshed authentication status or HTTP 401/validation failure.</returns>
        public async Task<IActionResult> OnGetKeepAliveAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var webLoginExpireMinutes = _adminUserInfoService.GetAdminWebLoginExpireMinutes();
            var jwtExpireMinutes = _adminUserInfoService.GetBackendJwtExpireMinutes();

            var cookieAuthResult = await HttpContext.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme);
            if (cookieAuthResult?.Succeeded == true && cookieAuthResult.Principal?.Identity?.IsAuthenticated == true)
            {
                var expiresUtc = await _adminUserInfoService.KeepCookieLoginAliveAsync(cookieAuthResult.Principal);
                return Ok(new
                {
                    authType = "cookie",
                    serverUtc = now,
                    expiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes,
                    token = string.Empty
                });
            }

            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
                {
                    return Ok(false, _localizer["Admin.Session.InvalidUser"].Value);
                }

                var tokenResult = await _adminUserInfoService.GenerateTokenAsync(userId, jwtExpireMinutes);
                return Ok(new
                {
                    authType = "jwt",
                    serverUtc = now,
                    expiresUtc = tokenResult.ExpiresUtc,
                    webLoginExpireMinutes,
                    jwtExpireMinutes,
                    token = tokenResult.Token
                });
            }

            return Unauthorized();
        }
    }
}
