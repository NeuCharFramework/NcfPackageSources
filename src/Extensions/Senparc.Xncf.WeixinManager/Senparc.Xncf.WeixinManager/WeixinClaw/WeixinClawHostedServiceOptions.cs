/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawHostedServiceOptions.cs
    文件功能描述：WeixinClawHostedServiceOptions 相关定义与实现


    创建标识：Senparc - 20261004

    修改标识：Senparc - 20261103
    修改描述：v0.24.16 add comprehensive tests for MCP endpoint management and background tenant scope

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

/// <summary>
/// 配置节：SenparcXncfWeixinManager:WeixinClaw:HostedService
/// </summary>
public sealed class WeixinClawHostedServiceOptions
{
    public const string SectionName = "SenparcXncfWeixinManager:WeixinClaw:HostedService";

    /// <summary>
    /// 后台账号扫描周期。
    /// </summary>
    public int ScanIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// getupdates 返回业务错误后的退避时间。
    /// </summary>
    public int ErrorRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// 停止宿主时等待后台任务退出的最大时间。
    /// </summary>
    public int ShutdownWaitSeconds { get; set; } = 5;

    /// <summary>
    /// 通知 Claw 停止时的单次 HTTP 超时时间。
    /// </summary>
    public int NotifyStopTimeoutSeconds { get; set; } = 2;

    public TimeSpan GetScanInterval() => TimeSpan.FromSeconds(Math.Clamp(ScanIntervalSeconds, 1, 300));

    public TimeSpan GetErrorRetryDelay() => TimeSpan.FromSeconds(Math.Clamp(ErrorRetryDelaySeconds, 1, 300));

    public TimeSpan GetShutdownWait() => TimeSpan.FromSeconds(Math.Clamp(ShutdownWaitSeconds, 1, 60));

    public TimeSpan GetNotifyStopTimeout() => TimeSpan.FromSeconds(Math.Clamp(NotifyStopTimeoutSeconds, 1, 30));
}
