/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDesktopAdminAuthTokenIssuer.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.5.0-preview4 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public interface IDesktopAdminAuthTokenIssuer
{
    Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
        int adminUserId,
        DateTimeOffset sourceAuthenticationExpiresUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed record DesktopAdminAuthTokenIssueResult(
    bool Succeeded,
    string? UserName = null,
    string? AccessToken = null,
    DateTimeOffset? ExpiresUtc = null,
    string? ErrorMessage = null);
