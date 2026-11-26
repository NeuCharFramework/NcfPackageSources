/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDesktopAdminAuthTokenIssuer.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.5.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
/// Issues short-lived administrator tokens for an approved DesktopBridge handoff.
/// </summary>
public interface IDesktopAdminAuthTokenIssuer
{
    /// <summary>
    /// Issues an access token constrained by the source authentication expiry.
    /// </summary>
    /// <param name="adminUserId">The administrator account receiving the token.</param>
    /// <param name="sourceAuthenticationExpiresUtc">The expiry of the source authenticated session.</param>
    /// <param name="cancellationToken">Token used to cancel issuance.</param>
    /// <returns>The token issue result and expiry metadata.</returns>
    Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
        int adminUserId,
        DateTimeOffset sourceAuthenticationExpiresUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of issuing a DesktopBridge administrator token.
/// </summary>
/// <param name="Succeeded">Whether a token was issued.</param>
/// <param name="UserName">The administrator name associated with the token.</param>
/// <param name="AccessToken">The short-lived access token.</param>
/// <param name="ExpiresUtc">The UTC expiry timestamp.</param>
/// <param name="ErrorMessage">The failure message when issuance fails.</param>
public sealed record DesktopAdminAuthTokenIssueResult(
    bool Succeeded,
    string? UserName = null,
    string? AccessToken = null,
    DateTimeOffset? ExpiresUtc = null,
    string? ErrorMessage = null);
