/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthHandoff.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

----------------------------------------------------------------*/

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Areas.DesktopBridge.Pages;

[Authorize(AuthenticationSchemes = "NcfAdminAuthorizeScheme", Policy = NcfAuthorizationPolicyNames.AdminOnly)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthHandoff : PageModel
{
    private readonly DesktopAdminAuthHandoffStore _handoffStore;
    private readonly IStringLocalizer<DesktopBridgeResource> _localizer;

    public AuthHandoff(
        DesktopAdminAuthHandoffStore handoffStore,
        IStringLocalizer<DesktopBridgeResource> localizer)
    {
        _handoffStore = handoffStore;
        _localizer = localizer;
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
            ErrorMessage = _localizer["AuthHandoff.RequestMissing"];
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplySecurityHeaders();
        if (RequestId == Guid.Empty || !_handoffStore.IsPending(RequestId))
        {
            ErrorMessage = _localizer["AuthHandoff.RequestMissing"];
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
            _handoffStore.Deny(RequestId, _localizer["AuthHandoff.LoginExpired"]);
            ErrorMessage = _localizer["AuthHandoff.LoginExpired"];
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(principal.FindFirstValue("TenantKey")))
        {
            _handoffStore.Deny(RequestId, _localizer["AuthHandoff.TenantUnsupported"]);
            ErrorMessage = _localizer["AuthHandoff.TenantUnsupported"];
            return Page();
        }

        if (cookieAuthentication.Properties?.ExpiresUtc is not { } cookieExpiresUtc ||
            cookieExpiresUtc <= DateTimeOffset.UtcNow.AddSeconds(10))
        {
            _handoffStore.Deny(RequestId, _localizer["AuthHandoff.LoginExpiring"]);
            ErrorMessage = _localizer["AuthHandoff.LoginExpiring"];
            return Page();
        }

        var userName = principal.Identity.Name ?? string.Empty;
        if (!_handoffStore.Approve(RequestId, adminUserId, userName, cookieExpiresUtc))
        {
            ErrorMessage = _localizer["AuthHandoff.RequestInvalidated"];
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
