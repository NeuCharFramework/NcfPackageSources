/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopAdminAuthHandoffModels.cs
    File description: Related implementation.

    Created by: Senparc - 20260804

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

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
