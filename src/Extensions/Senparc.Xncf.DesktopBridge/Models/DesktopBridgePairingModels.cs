/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgePairingModels.cs
    文件功能描述：DesktopBridge 设备配对和会话管理协议

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

namespace Senparc.Xncf.DesktopBridge.Models;

public sealed record DesktopBridgePairingCreateRequest(string? ClientName);

public sealed record DesktopBridgePairingCreateResponse(
    Guid RequestId,
    string DeviceCode,
    string PollSecret,
    DateTimeOffset ExpiresAt,
    string VerificationPath,
    int PollIntervalSeconds);

public sealed record DesktopBridgePairingPollRequest(Guid RequestId, string? PollSecret);

public sealed record DesktopBridgePairingPollResponse(
    string Status,
    string? SessionToken = null,
    DateTimeOffset? SessionExpiresAt = null,
    string? Message = null);

public sealed record DesktopBridgePendingPairingView(
    Guid RequestId,
    string DeviceCode,
    string ClientName,
    string RemoteAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public sealed record DesktopBridgeSessionView(
    Guid SessionId,
    string ClientName,
    string RemoteAddress,
    string ApprovedBy,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt);

