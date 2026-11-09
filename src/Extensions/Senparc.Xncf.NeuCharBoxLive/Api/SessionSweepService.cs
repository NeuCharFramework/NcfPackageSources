/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SessionSweepService.cs
    文件功能描述：后台服务：每分钟清理空闲过期的会话并结束其事件流


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 修正注释：配置有误不再让宿主停下，而是在启动时记录错误并关闭 Live（config）

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// Expires idle sessions every minute on the registered TimeProvider (SessionManager.Sweep), which also ends their SSE
/// streams. Taking SessionManager here builds it, and so the ILiveRuntime, when the host starts: options that can't work
/// are logged as an error right then (and live is off with config), not at the first request. They never stop the host.
/// </summary>
public sealed class SessionSweepService(SessionManager sessions, TimeProvider tp, ILogger<SessionSweepService> log) : BackgroundService
{
    public static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Every, tp);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { sessions.Sweep(); }
                catch (Exception e) { log.LogError(e, "Sweeping the live sessions failed"); } // try again next minute
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
