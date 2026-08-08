/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IDesktopAdminAuthTokenIssuer.cs
    文件功能描述：桌面端管理员一次性换票所需的最小令牌签发契约

    创建标识：Senparc - 20260804

    修改标识：Senparc - 20260808
    修改描述：v0.5.0-preview4 新增桌面管理员一次性换票签发契约

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Security;

/// <summary>
/// 由管理员模块实现、由可选桌面桥接模块调用的短期令牌签发契约。
/// 调用方必须先完成桌面会话、一次性挑战和浏览器 Cookie 身份校验。
/// </summary>
public interface IDesktopAdminAuthTokenIssuer
{
    Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
        int adminUserId,
        DateTimeOffset sourceAuthenticationExpiresUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 一次性换票结果。访问令牌只能返回给已通过桌面会话和 PKCE 校验的调用方。
/// </summary>
public sealed record DesktopAdminAuthTokenIssueResult(
    bool Succeeded,
    string? UserName = null,
    string? AccessToken = null,
    DateTimeOffset? ExpiresUtc = null,
    string? ErrorMessage = null);
