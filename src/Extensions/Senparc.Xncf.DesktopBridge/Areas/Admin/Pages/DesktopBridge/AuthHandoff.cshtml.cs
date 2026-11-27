/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthHandoff.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.12.2 Improve source documentation, headers, and module metadata consistency.

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

    /// <summary>
    /// Initializes the administrator approval page for desktop authentication handoffs.
    /// </summary>
    /// <param name="handoffStore">The handoff request store.</param>
    /// <param name="localizer">The localized message provider.</param>
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

    /// <summary>
    /// Loads whether the requested handoff is still pending and applies no-store security headers.
    /// </summary>
    public void OnGet()
    {
        ApplySecurityHeaders();
        CanSubmit = RequestId != Guid.Empty && _handoffStore.IsPending(RequestId);
        if (!CanSubmit)
        {
            ErrorMessage = _localizer["AuthHandoff.RequestMissing"];
        }
    }

    /// <summary>
    /// Authenticates the administrator, approves a valid handoff, or returns a localized failure page.
    /// </summary>
    /// <returns>A local redirect after approval or the current page on failure.</returns>
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

    /// <summary>
    /// Applies no-cache, referrer, framing, and content-security headers to the handoff page.
    /// </summary>
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
