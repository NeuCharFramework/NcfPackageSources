/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Settings.cshtml.cs
    文件功能描述：NeuCharBox Live 管理端页面（Admin）：Settings.cshtml


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增设置页：状态、今日用量、试用链接，以及开关、模型、限额、额外来源的表单

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Areas.Admin.Pages.NeuCharBoxLive;

/// <summary>
/// The module's home page: whether live is on and why not, today's token spend against the budget, a link to try the demo
/// against this site, and the form for the settings row (<see cref="LiveSettingsService"/>). The logic is in
/// <see cref="SettingsView"/>; this class reads and forwards. The AI Kernel model list reaches the page as
/// <see cref="ModelChoice"/> only (Id, label, supported): no key ever leaves the server.
/// </summary>
public class SettingsModel(Lazy<XncfModuleService> xncfModuleService, LiveSettingsService settings, IAiModelSource models,
    ILiveRuntime runtime, ITokenMeter meter, IRunStore runs, UsageDay usageDay, TimeProvider tp)
    : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>What the form shows: the saved settings, or, after a refused save, what was typed.</summary>
    public LiveSettings Form { get; private set; } = LiveSettings.Defaults;

    /// <summary>The extra-origins textarea text as typed (one per line).</summary>
    public string OriginsText { get; private set; } = "";

    public IReadOnlyList<ModelChoice> Choices { get; private set; } = Array.Empty<ModelChoice>();
    public LiveSnapshot Snapshot { get; private set; } = LiveSnapshot.Off(LiveOff.Starting, []);
    public bool BudgetSpent { get; private set; }
    public long TokensToday { get; private set; }
    public DateOnly Today { get; private set; }

    /// <summary>The property names a refused save found wrong (SettingsView/LiveSettingsService names); empty otherwise.</summary>
    public IReadOnlyList<string> Problems { get; private set; } = Array.Empty<string>();

    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

    public string UsageZone => usageDay.Zone.Id;
    public string StatusKey => SettingsView.StatusKey(Snapshot, BudgetSpent);
    public bool IsOn => StatusKey == "Status.On";
    public long DailyTokenBudget => Snapshot.Limits.DailyTokenBudget;
    public bool PipelineHooked => LiveApiPipeline.Hooked;

    /// <summary>The demo link for this site, or null when the site is not plain-http loopback.</summary>
    public string? TryItLink => SettingsView.TryItLink($"{Request.Scheme}://{Request.Host}");

    public async Task OnGetAsync()
    {
        Form = await settings.GetAsync(HttpContext.RequestAborted);
        OriginsText = string.Join("\n", Form.ExtraOrigins);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(bool enabled, int? planModelId, int? runModelId, int? perCodeDaily, int? globalDaily,
        long? dailyTokenBudget, string? extraOrigins)
    {
        var ct = HttpContext.RequestAborted;
        Choices = SettingsView.Choices(await models.ListAsync(ct));
        // A blank or non-numeric limit becomes 0, which Validate refuses.
        Form = new LiveSettings(enabled, planModelId, runModelId, perCodeDaily ?? 0, globalDaily ?? 0, dailyTokenBudget ?? 0,
            SettingsView.ParseOrigins(extraOrigins));
        OriginsText = extraOrigins ?? "";

        var problems = await SettingsView.SaveAsync(settings, Form, Choices, ct);
        if (problems.Count == 0) return RedirectToPage(new { uid = Uid, saved = true });

        Problems = problems;
        await LoadAsync();
        return Page();
    }

    async Task LoadAsync()
    {
        var ct = HttpContext.RequestAborted;
        if (Choices.Count == 0) Choices = SettingsView.Choices(await models.ListAsync(ct));
        Snapshot = runtime.Current;
        Today = usageDay.For(tp.GetUtcNow());
        BudgetSpent = meter.Spent(Today);
        TokensToday = await runs.TokensTodayAsync(Today);
    }
}
