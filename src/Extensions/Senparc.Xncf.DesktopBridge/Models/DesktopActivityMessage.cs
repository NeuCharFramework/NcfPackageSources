/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopActivityMessage.cs
    文件功能描述：桌面状态桥接协议模型

    创建标识：Senparc - 20260725
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
    string SnapshotEndpoint);
