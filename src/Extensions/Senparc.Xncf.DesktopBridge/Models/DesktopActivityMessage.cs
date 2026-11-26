/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopActivityMessage.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.DesktopBridge.Models;

/// <summary>
/// Represents one ordered desktop activity update exposed through the bridge.
/// </summary>
/// <param name="Sequence">The monotonically increasing stream sequence.</param>
/// <param name="ActivityId">The stable activity identifier.</param>
/// <param name="Source">The subsystem that emitted the activity.</param>
/// <param name="State">The current activity state.</param>
/// <param name="Title">The short activity title.</param>
/// <param name="Detail">Optional activity detail.</param>
/// <param name="Progress">Optional progress value, normally between zero and one.</param>
/// <param name="Time">The UTC timestamp of the update.</param>
/// <param name="IsTerminal">Whether the activity has reached a terminal state.</param>
/// <param name="ActionUrl">Optional URL for a user action related to the activity.</param>
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

/// <summary>
/// Describes the protocol endpoints and capabilities exposed by DesktopBridge.
/// </summary>
/// <param name="ProtocolVersion">The bridge protocol version.</param>
/// <param name="BridgeVersion">The installed bridge version.</param>
/// <param name="SupportsSse">Whether server-sent event streaming is available.</param>
/// <param name="SupportsSnapshot">Whether activity snapshots are available.</param>
/// <param name="EventEndpoint">The relative event-stream endpoint.</param>
/// <param name="SnapshotEndpoint">The relative snapshot endpoint.</param>
/// <param name="SupportsAuthorizedSync">Whether authorized integration synchronization is available.</param>
/// <param name="AuthorizedSyncEndpoint">The optional authorized-sync endpoint.</param>
/// <param name="SupportsAdminAuthHandoff">Whether desktop admin-auth handoff is available.</param>
/// <param name="AdminAuthHandoffRequestEndpoint">The optional handoff-request endpoint.</param>
/// <param name="AdminAuthHandoffRedeemEndpoint">The optional handoff-redeem endpoint.</param>
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
/// Represents one authorized synchronization update delivered to a desktop client.
/// </summary>
/// <param name="Sequence">The monotonically increasing synchronization sequence.</param>
/// <param name="Channel">The source integration channel.</param>
/// <param name="ResourceId">The synchronized resource identifier.</param>
/// <param name="Action">The synchronization action.</param>
/// <param name="Time">The UTC timestamp of the update.</param>
public sealed record DesktopAuthorizedSyncMessage(
    long Sequence,
    string Channel,
    string ResourceId,
    string Action,
    DateTimeOffset Time);
