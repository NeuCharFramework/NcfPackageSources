/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AuthHandoff.cshtml.cs
    文件功能描述：用当前 WebView 管理员 Cookie 确认一次性桌面授权

    创建标识：Senparc - 20260804

    修改标识：Senparc - 20260808
    修改描述：v0.4.0-preview4 新增 WebView 管理员 Cookie 一次性授权确认页

----------------------------------------------------------------*/

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Areas.DesktopBridge.Pages;

[Authorize(AuthenticationSchemes = "NcfAdminAuthorizeScheme", Policy = NcfAuthorizationPolicyNames.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthHandoff : PageModel
{
    private readonly DesktopAdminAuthHandoffStore _handoffStore;

    public AuthHandoff(DesktopAdminAuthHandoffStore handoffStore)
    {
        _handoffStore = handoffStore;
    }

    [BindProperty(SupportsGet = true)]
    public Guid RequestId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool CanSubmit { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        ApplySecurityHeaders();
        CanSubmit = RequestId != Guid.Empty && _handoffStore.IsPending(RequestId);
        if (!CanSubmit)
        {
            ErrorMessage = DesktopBridgeResource.Get("Auth.Error.RequestExpired");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplySecurityHeaders();
        if (RequestId == Guid.Empty || !_handoffStore.IsPending(RequestId))
        {
            ErrorMessage = DesktopBridgeResource.Get("Auth.Error.RequestExpired");
            return Page();
        }

        var cookieAuthentication = await HttpContext
            .AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme)
            .ConfigureAwait(false);
        var principal = cookieAuthentication.Principal;
        if (!cookieAuthentication.Succeeded || principal?.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId) ||
            adminUserId <= 0)
        {
            var errorMessage = DesktopBridgeResource.Get("Auth.Error.LoginExpired");
            _handoffStore.Deny(RequestId, errorMessage);
            ErrorMessage = errorMessage;
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(principal.FindFirstValue("TenantKey")))
        {
            _handoffStore.Deny(RequestId, DesktopBridgeResource.Get("Auth.Error.MultiTenantDenied"));
            ErrorMessage = DesktopBridgeResource.Get("Auth.Error.MultiTenantRetry");
            return Page();
        }

        if (cookieAuthentication.Properties?.ExpiresUtc is not { } cookieExpiresUtc ||
            cookieExpiresUtc <= DateTimeOffset.UtcNow.AddSeconds(10))
        {
            var errorMessage = DesktopBridgeResource.Get("Auth.Error.LoginExpiring");
            _handoffStore.Deny(RequestId, errorMessage);
            ErrorMessage = errorMessage;
            return Page();
        }

        var userName = principal.Identity.Name ?? string.Empty;
        if (!_handoffStore.Approve(RequestId, adminUserId, userName, cookieExpiresUtc))
        {
            ErrorMessage = DesktopBridgeResource.Get("Auth.Error.RequestInvalid");
            return Page();
        }

        var returnUrl = Url.IsLocalUrl(ReturnUrl) &&
                        !ReturnUrl!.StartsWith("/Admin/DesktopBridge/AuthHandoff", StringComparison.OrdinalIgnoreCase)
            ? ReturnUrl
            : "/Admin/Index";
        return LocalRedirect(returnUrl);
    }

    private void ApplySecurityHeaders()
    {
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; script-src 'none'; style-src 'unsafe-inline'; " +
            "form-action 'self'; base-uri 'none'; frame-ancestors 'self'";
    }
}
