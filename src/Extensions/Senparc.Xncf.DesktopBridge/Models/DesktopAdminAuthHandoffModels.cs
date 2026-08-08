/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopAdminAuthHandoffModels.cs
    文件功能描述：WebView Cookie 到桌面 Admin JWT 的一次性换票协议

    创建标识：Senparc - 20260804

    修改标识：Senparc - 20260808
    修改描述：v0.4.0-preview4 新增桌面管理员一次性换票协议模型

----------------------------------------------------------------*/

namespace Senparc.Xncf.DesktopBridge.Models;

public sealed record DesktopAdminAuthHandoffCreateRequest(
    string? CodeChallenge,
    string? ReturnPath);

public sealed record DesktopAdminAuthHandoffCreateResponse(
    Guid RequestId,
    DateTimeOffset ExpiresAt,
    string ApprovalPath,
    int PollIntervalMilliseconds);

public sealed record DesktopAdminAuthHandoffRedeemRequest(
    Guid RequestId,
    string? CodeVerifier);

public sealed record DesktopAdminAuthHandoffRedeemResponse(
    string Status,
    string? UserName = null,
    string? AccessToken = null,
    DateTimeOffset? ExpiresUtc = null,
    string? Message = null);
