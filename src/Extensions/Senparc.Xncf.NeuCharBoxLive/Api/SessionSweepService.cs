/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SessionSweepService.cs
    文件功能描述：NeuCharBox Live HTTP 接口层：SessionSweepService


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// Expires idle sessions every minute on the registered TimeProvider (SessionManager.Sweep), which also ends their SSE
/// streams. Taking SessionManager here builds it when the host starts, so bad options stop the host at once.
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
