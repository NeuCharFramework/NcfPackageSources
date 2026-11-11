/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Settings.cshtml.cs
    文件功能描述：后台“设置”页（模块首页）：状态、今日用量、试用链接，及启用、模型、限额与额外来源的表单


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增设置页：状态、今日用量、试用链接，以及开关、模型、限额、额外来源的表单

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 AI Kernel 读不到时显示空的模型列表与提示，不再抛错；保存成功后先等运行时刷新再跳转

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 打开设置页时唤醒仍在 starting 的运行时（本次运行中新安装的站点无需重启）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 “试用”链接改指向本站自带的演示页

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
/// <see cref="ModelChoice"/> only (Id, label, supported): no key ever leaves the server. When AI Kernel can't be read the
/// pickers are empty and the page says so; it never fails for it.
/// </summary>
public class SettingsModel(Lazy<XncfModuleService> xncfModuleService, LiveSettingsService settings, IAiModelSource models,
    ILiveRuntime runtime, ITokenMeter meter, IRunStore runs, UsageDay usageDay, TimeProvider tp, ILogger<SettingsModel> log)
    : AdminXncfModulePageModelBase(xncfModuleService)
{
    bool _choicesLoaded;

    /// <summary>What the form shows: the saved settings, or, after a refused save, what was typed.</summary>
    public LiveSettings Form { get; private set; } = LiveSettings.Defaults;

    /// <summary>The extra-origins textarea text as typed (one per line).</summary>
    public string OriginsText { get; private set; } = "";

    public IReadOnlyList<ModelChoice> Choices { get; private set; } = Array.Empty<ModelChoice>();

    /// <summary>AI Kernel's models couldn't be read (not installed or not enabled, or its read failed): the pickers are empty.</summary>
    public bool AiKernelUnreadable { get; private set; }

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

    /// <summary>The demo pages this site serves itself.</summary>
    public string TryItLink => SettingsView.TryItLink(Request.PathBase);

    public async Task OnGetAsync()
    {
        // A site installed in this run: the runtime waits for NCF's readiness, which only a restart brings. This page just
        // read the database, so a refresh here wakes it (the runtime's own state, not the config wrapper's).
        if (HttpContext.RequestServices.GetService<ModuleLiveRuntime>() is { } module)
            await SettingsView.WakeIfStartingAsync(module.Current, module.RefreshAsync, HttpContext.RequestAborted);
        Form = await settings.GetAsync(HttpContext.RequestAborted);
        OriginsText = string.Join("\n", Form.ExtraOrigins);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(bool enabled, int? planModelId, int? runModelId, int? perCodeDaily, int? globalDaily,
        long? dailyTokenBudget, string? extraOrigins)
    {
        var ct = HttpContext.RequestAborted;
        await LoadChoicesAsync(ct);
        // A blank or non-numeric limit becomes 0, which Validate refuses.
        Form = new LiveSettings(enabled, planModelId, runModelId, perCodeDaily ?? 0, globalDaily ?? 0, dailyTokenBudget ?? 0,
            SettingsView.ParseOrigins(extraOrigins));
        OriginsText = extraOrigins ?? "";

        // Only this call takes the concrete runtime: the saved settings are in the status line of the page redirected to.
        Func<CancellationToken, Task>? refresh = null;
        if (HttpContext.RequestServices.GetService<ModuleLiveRuntime>() is { } module) refresh = module.RefreshAsync;
        var problems = await SettingsView.SaveAsync(settings, Form, Choices, ct, refresh);
        if (problems.Count == 0) return RedirectToPage(new { uid = Uid, saved = true });

        Problems = problems;
        await LoadAsync();
        return Page();
    }

    async Task LoadChoicesAsync(CancellationToken ct)
    {
        if (_choicesLoaded) return;
        (Choices, AiKernelUnreadable) = await SettingsView.LoadChoicesAsync(models, log, ct);
        _choicesLoaded = true;
    }

    async Task LoadAsync()
    {
        var ct = HttpContext.RequestAborted;
        await LoadChoicesAsync(ct);
        Snapshot = runtime.Current;
        Today = usageDay.For(tp.GetUtcNow());
        BudgetSpent = meter.Spent(Today);
        TokensToday = await runs.TokensTodayAsync(Today);
    }
}
