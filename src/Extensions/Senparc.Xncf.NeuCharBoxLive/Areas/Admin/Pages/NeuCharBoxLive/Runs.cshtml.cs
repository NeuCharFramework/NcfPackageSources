/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Runs.cshtml.cs
    文件功能描述：NeuCharBox Live 管理端页面（Admin）：Runs.cshtml


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 页面文字改走资源文件，时间改用使用日所在时区

----------------------------------------------------------------*/

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;

namespace Senparc.Xncf.NeuCharBoxLive.Areas.Admin.Pages.NeuCharBoxLive;

/// <summary>
/// The run log (spec 7.3): the latest <see cref="Shown"/> runs, today's token spend against the runtime's current
/// DailyTokenBudget (spec 4.4), totals by model for today and all time, and <c>?handler=Csv</c>, every run as CSV for
/// the handover cost estimate.
/// </summary>
public class RunsModel(Lazy<XncfModuleService> xncfModuleService, IRunStore runs, EfLiveStore store, ILiveRuntime runtime,
    UsageDay usageDay, TimeProvider tp) : AdminXncfModulePageModelBase(xncfModuleService)
{
    public const int Shown = 200;

    public IReadOnlyList<RunRow> Rows { get; private set; } = Array.Empty<RunRow>();
    public IReadOnlyList<RunTotals> TodayTotals { get; private set; } = Array.Empty<RunTotals>();
    public IReadOnlyList<RunTotals> AllTotals { get; private set; } = Array.Empty<RunTotals>();
    public DateOnly Today { get; private set; }
    public DateTimeOffset Now { get; private set; }

    /// <summary>TokensTodayAsync(today): finished runs only (the kill switch's meter also counts runs in flight).</summary>
    public long TokensToday { get; private set; }

    public long DailyTokenBudget => runtime.Current.Limits.DailyTokenBudget;

    /// <summary>The time zone whose midnight starts a usage day (NcbLive:UsageTimeZone).</summary>
    public string UsageZone => usageDay.Zone.Id;

    public TimeZoneInfo Zone => usageDay.Zone;

    public async Task OnGetAsync()
    {
        Now = tp.GetUtcNow();
        Today = usageDay.For(Now);
        Rows = await runs.ListAsync(Shown);
        TokensToday = await runs.TokensTodayAsync(Today);
        TodayTotals = await store.TotalsAsync(Today);
        AllTotals = await store.TotalsAsync(null);
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        var csv = RunsCsv.Write(await runs.ListAsync(int.MaxValue));
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(); // BOM: Excel reads UTF-8
        return File(bytes, "text/csv; charset=utf-8", $"ncb-live-runs-{usageDay.For(tp.GetUtcNow()):yyyy-MM-dd}.csv");
    }

    /// <summary>The sum over models, or null when there is at most one model (its own row says it all).</summary>
    public static RunTotals? Sum(IReadOnlyList<RunTotals> rows, string label) => rows.Count < 2 ? null :
        new RunTotals(label, rows.Sum(r => r.Runs), rows.Sum(r => r.WhatIfs), rows.Sum(r => r.PromptTokens),
            rows.Sum(r => r.CompletionTokens));
}
