/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopAdminAuthTokenIssuer.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.Security;

namespace Senparc.Areas.Admin.Domain;

public sealed class DesktopAdminAuthTokenIssuer : IDesktopAdminAuthTokenIssuer
{
    private readonly AdminUserInfoService _adminUserInfoService;

    public DesktopAdminAuthTokenIssuer(AdminUserInfoService adminUserInfoService)
    {
        _adminUserInfoService = adminUserInfoService;
    }

    public async Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
        int adminUserId,
        DateTimeOffset sourceAuthenticationExpiresUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUserId <= 0 || sourceAuthenticationExpiresUtc <= DateTimeOffset.UtcNow.AddSeconds(10))
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "WebView 管理员登录已过期，请重新登录。");
        }

        var adminUser = await _adminUserInfoService
            .GetObjectAsync(user => user.Id == adminUserId)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (adminUser == null)
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "管理员账号不存在或已失效。");
        }

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        if (adminUser.TenantId > 0)
        {
            return new DesktopAdminAuthTokenIssueResult(
                false,
                ErrorMessage: "多租户管理员暂不支持 WebView 自动授权，请使用显式登录。");
        }

        var token = await _adminUserInfoService
            .GenerateTokenForDesktopHandoffAsync(adminUser.Id, sourceAuthenticationExpiresUtc)
            .ConfigureAwait(false);
        return new DesktopAdminAuthTokenIssueResult(
            true,
            adminUser.UserName,
            token.Token,
            token.ExpiresUtc);
    }
}
