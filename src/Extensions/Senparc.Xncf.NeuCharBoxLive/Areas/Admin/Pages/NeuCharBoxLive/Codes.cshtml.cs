/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Codes.cshtml.cs
    文件功能描述：NeuCharBox Live 管理端页面（Admin）：Codes.cshtml


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Areas.Admin.Pages.NeuCharBoxLive;

/// <summary>Invite codes (spec 7.3): list, create by hand (label and daily limit), set the limit, disable or enable.</summary>
public class CodesModel(Lazy<XncfModuleService> xncfModuleService, IInviteStore codes, EfLiveStore store, ILiveRuntime runtime,
    UsageDay usageDay, TimeProvider tp) : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>Far above the global daily cap (GlobalDaily), so it never limits anything real.</summary>
    public const int MaxLimit = 1000;

    public IReadOnlyList<InviteCode> Codes { get; private set; } = Array.Empty<InviteCode>();
    public IReadOnlyDictionary<string, int> UsesToday { get; private set; } = new Dictionary<string, int>();
    public DateOnly Today { get; private set; }

    /// <summary>The time zone whose midnight starts a usage day (NcbLive:UsageTimeZone).</summary>
    public string UsageZone => usageDay.Zone.Id;

    public int DefaultLimit => runtime.Current.Limits.PerCodeDaily;
    public string? Error { get; private set; }
    public string? NewLabel { get; private set; }
    public int? NewLimit { get; private set; }

    /// <summary>The code just created: its row is highlighted.</summary>
    [BindProperty(SupportsGet = true)]
    public long? Created { get; set; }

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync(string? label, int? limit)
    {
        label = label?.Trim() ?? "";
        if (label.Length is 0 or > LiveColumns.Label)
            return await InvalidAsync($"The label is required and at most {LiveColumns.Label} characters.", label, limit);
        if (limit is not (>= 1 and <= MaxLimit))
            return await InvalidAsync($"The daily limit is a whole number from 1 to {MaxLimit}.", label, limit);
        var c = await codes.CreateAsync(label, limit.Value);
        return RedirectToPage(new { uid = Uid, created = c.Id });
    }

    public async Task<IActionResult> OnPostLimitAsync(long id, int? limit)
    {
        if (limit is not (>= 1 and <= MaxLimit))
            return await InvalidAsync($"The daily limit is a whole number from 1 to {MaxLimit}.", null, null);
        try { await codes.SetLimitAsync(id, limit.Value); }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToPage(new { uid = Uid });
    }

    public async Task<IActionResult> OnPostDisabledAsync(long id, bool disabled)
    {
        try { await codes.SetDisabledAsync(id, disabled); }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToPage(new { uid = Uid });
    }

    async Task<IActionResult> InvalidAsync(string error, string? label, int? limit)
    {
        Error = error;
        NewLabel = label;
        NewLimit = limit;
        await LoadAsync();
        return Page();
    }

    async Task LoadAsync()
    {
        Today = usageDay.For(tp.GetUtcNow());
        Codes = await codes.ListAsync();
        UsesToday = await store.UsesOnAsync(Today);
    }
}
