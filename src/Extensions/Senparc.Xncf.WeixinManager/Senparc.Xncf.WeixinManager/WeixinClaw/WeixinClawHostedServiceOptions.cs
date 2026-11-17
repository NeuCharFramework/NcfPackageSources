/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinClawHostedServiceOptions.cs
    File description: WeixinClawHostedServiceOptions related definitions and implementation


    Created by: Senparc - 20261004

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add comprehensive tests for MCP endpoint management and background tenant scope

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class WeixinClawHostedServiceOptions
{
    public const string SectionName = "SenparcXncfWeixinManager:WeixinClaw:HostedService";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int ErrorRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int ShutdownWaitSeconds { get; set; } = 5;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public int NotifyStopTimeoutSeconds { get; set; } = 2;

    public TimeSpan GetScanInterval() => TimeSpan.FromSeconds(Math.Clamp(ScanIntervalSeconds, 1, 300));

    public TimeSpan GetErrorRetryDelay() => TimeSpan.FromSeconds(Math.Clamp(ErrorRetryDelaySeconds, 1, 300));

    public TimeSpan GetShutdownWait() => TimeSpan.FromSeconds(Math.Clamp(ShutdownWaitSeconds, 1, 60));

    public TimeSpan GetNotifyStopTimeout() => TimeSpan.FromSeconds(Math.Clamp(NotifyStopTimeoutSeconds, 1, 30));
}
