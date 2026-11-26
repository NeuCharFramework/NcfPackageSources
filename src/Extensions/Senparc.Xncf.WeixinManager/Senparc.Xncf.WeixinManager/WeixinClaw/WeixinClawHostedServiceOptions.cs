/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawHostedServiceOptions.cs
    File description: WeixinClawHostedServiceOptions related definitions and implementation

    Created by: Senparc - 20261004

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add comprehensive tests for MCP endpoint management and background tenant scope

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
/// Configures the polling, retry, and shutdown timing used by the Weixin Claw hosted service.
/// Values are clamped by the conversion methods to protect the background loop from invalid
/// or excessively long configuration values.
/// </summary>
public sealed class WeixinClawHostedServiceOptions
{
    public const string SectionName = "SenparcXncfWeixinManager:WeixinClaw:HostedService";

    /// <summary>
    /// Gets or sets the interval between account-discovery scans, in seconds.
    /// </summary>
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the delay before retrying an account after a polling error, in seconds.
    /// </summary>
    public int ErrorRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum wait for hosted-service shutdown, in seconds.
    /// </summary>
    public int ShutdownWaitSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout used when notifying an account polling task to stop, in seconds.
    /// </summary>
    public int NotifyStopTimeoutSeconds { get; set; } = 2;

    /// <summary>
    /// Converts the configured scan interval to a bounded duration between one and 300 seconds.
    /// </summary>
    /// <returns>The effective account-scan interval.</returns>
    public TimeSpan GetScanInterval() => TimeSpan.FromSeconds(Math.Clamp(ScanIntervalSeconds, 1, 300));

    /// <summary>
    /// Converts the configured error retry delay to a bounded duration between one and 300 seconds.
    /// </summary>
    /// <returns>The effective retry delay.</returns>
    public TimeSpan GetErrorRetryDelay() => TimeSpan.FromSeconds(Math.Clamp(ErrorRetryDelaySeconds, 1, 300));

    /// <summary>
    /// Converts the configured shutdown wait to a bounded duration between one and 60 seconds.
    /// </summary>
    /// <returns>The effective shutdown wait.</returns>
    public TimeSpan GetShutdownWait() => TimeSpan.FromSeconds(Math.Clamp(ShutdownWaitSeconds, 1, 60));

    /// <summary>
    /// Converts the configured account-stop timeout to a bounded duration between one and 30 seconds.
    /// </summary>
    /// <returns>The effective account-stop notification timeout.</returns>
    public TimeSpan GetNotifyStopTimeout() => TimeSpan.FromSeconds(Math.Clamp(NotifyStopTimeoutSeconds, 1, 30));
}
