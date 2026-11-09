/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RetentionService.cs
    文件功能描述：后台服务：访客文字保留期（30 天）之后清空请求文本、计划与总结


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 运行时为 starting（NCF 未就绪）、not_installed 或 multi_tenant 时跳过本次清理，一分钟后再看

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>
/// Visitor text retention (spec 7.3): at start and then every <see cref="Period"/>, blanks the request text, plan and
/// summary of runs started more than <see cref="Keep"/> ago. Token, call and latency numbers stay. A failed pass is logged
/// and retried at the next period; it never stops the host.
/// <para>
/// Ruling 14: a pass is skipped, and looked at again after <see cref="Retry"/>, while <paramref name="runtime"/> (the
/// module's own runtime, not AddNcbLive's config wrapper) is <see cref="LiveOff.Starting"/> (NCF hasn't reported the site
/// ready: the database may be in maintenance or on an old schema), <see cref="LiveOff.NotInstalled"/> (the table may not
/// exist) or <see cref="LiveOff.MultiTenant"/> (reads would be tenant-filtered). Reading the snapshot costs nothing.
/// </para>
/// </summary>
public sealed class RetentionService(IRunStore runs, ILiveRuntime runtime, TimeProvider tp, ILogger<RetentionService> log) : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromHours(6);
    public static readonly TimeSpan Keep = TimeSpan.FromDays(30);

    /// <summary>How soon a skipped pass is looked at again.</summary>
    public static readonly TimeSpan Retry = TimeSpan.FromMinutes(1);

    static readonly EventId PassFailed = new(1301, "RetentionPassFailed");

    /// <summary>
    /// Ticks every <see cref="Retry"/> on a timer made before the first pass (so no tick is missed), and passes when one is
    /// due: at start, then <see cref="Period"/> after the last pass that ran. A skipped pass leaves it due.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Retry, tp);
            DateTimeOffset? due = null; // now
            do
            {
                var now = tp.GetUtcNow();
                if ((due is null || now >= due) && await PassAsync()) due = now + Period;
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>False when the pass was skipped (see the class); true when it ran, whether or not it failed.</summary>
    async Task<bool> PassAsync()
    {
        if (runtime.Current.OffReason is LiveOff.Starting or LiveOff.NotInstalled or LiveOff.MultiTenant) return false;
        try
        {
            await runs.BlankTextOlderThanAsync(tp.GetUtcNow() - Keep);
        }
        catch (Exception e)
        {
            log.LogWarning(PassFailed, e, "Retention pass failed; it runs again in {Period}", Period);
        }
        return true;
    }
}
