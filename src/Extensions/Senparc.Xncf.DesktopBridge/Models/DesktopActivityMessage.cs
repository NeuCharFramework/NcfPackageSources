/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopActivityMessage.cs
    文件功能描述：桌面状态桥接协议模型

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

    修改标识：Senparc - 20260808
    修改描述：v0.4.0-preview4 扩展活动消息以支持管理员换票交接状态

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
/// 受身份隔离的资源变更通知。只提供重新读取数据所需的最小元数据。
/// </summary>
public sealed record DesktopAuthorizedSyncMessage(
    long Sequence,
    string Channel,
    string ResourceId,
    string Action,
    DateTimeOffset Time);
