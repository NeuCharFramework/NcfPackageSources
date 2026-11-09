/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RetentionService.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Services）：RetentionService


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>
/// Visitor text retention (spec 7.3): at start and then every <see cref="Period"/>, blanks the request text, plan and
/// summary of runs started more than <see cref="Keep"/> ago. Token, call and latency numbers stay. A failed pass (before
/// the module is installed its table doesn't exist) is logged and retried at the next tick; it never stops the host.
/// </summary>
public sealed class RetentionService(IRunStore runs, TimeProvider tp, ILogger<RetentionService> log) : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromHours(6);
    public static readonly TimeSpan Keep = TimeSpan.FromDays(30);

    static readonly EventId PassFailed = new(1301, "RetentionPassFailed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Period, tp);
            do await PassAsync();
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    async Task PassAsync()
    {
        try
        {
            await runs.BlankTextOlderThanAsync(tp.GetUtcNow() - Keep);
        }
        catch (Exception e)
        {
            log.LogWarning(PassFailed, e, "Retention pass failed; it runs again in {Period}", Period);
        }
    }
}
