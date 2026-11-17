/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopActivityMessage.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

----------------------------------------------------------------*/

namespace Senparc.Xncf.DesktopBridge.Models;

public sealed record DesktopActivityMessage(
    long Sequence,
    string ActivityId,
    string Source,
    string State,
    string Title,
    string? Detail,
    double? Progress,
    DateTimeOffset Time,
    bool IsTerminal,
    string? ActionUrl);

public sealed record DesktopBridgeCapabilities(
    int ProtocolVersion,
    string BridgeVersion,
    bool SupportsSse,
    bool SupportsSnapshot,
    string EventEndpoint,
    string SnapshotEndpoint,
    bool SupportsAuthorizedSync,
    string? AuthorizedSyncEndpoint,
    bool SupportsAdminAuthHandoff = false,
    string? AdminAuthHandoffRequestEndpoint = null,
    string? AdminAuthHandoffRedeemEndpoint = null);

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed record DesktopAuthorizedSyncMessage(
    long Sequence,
    string Channel,
    string ResourceId,
    string Action,
    DateTimeOffset Time);
