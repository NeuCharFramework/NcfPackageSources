/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Applications.cshtml.cs
    文件功能描述：NeuCharBox Live 管理端页面（Admin）：Applications.cshtml


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 页面文字改走资源文件，时间改用使用日所在时区

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;

namespace Senparc.Xncf.NeuCharBoxLive.Areas.Admin.Pages.NeuCharBoxLive;

/// <summary>
/// Applications, newest first (spec 7.3). Approve makes a code labelled with the email (daily limit: the runtime's
/// current PerCodeDaily) and the row then shows the ready-to-copy message; Decline sets the status. The server sends
/// no email: Leo does.
/// </summary>
public class ApplicationsModel(Lazy<XncfModuleService> xncfModuleService, IApplicationStore applications, IInviteStore codes,
    ILiveRuntime runtime, UsageDay usageDay) : AdminXncfModulePageModelBase(xncfModuleService)
{
    public sealed record Row(Application App, InviteCode? Code, string? Message);

    public IReadOnlyList<Row> Rows { get; private set; } = Array.Empty<Row>();

    /// <summary>The time zone whose midnight starts a usage day; admin times are shown in it.</summary>
    public string UsageZone => usageDay.Zone.Id;

    public TimeZoneInfo Zone => usageDay.Zone;

    /// <summary>The daily limit a code made on approval starts with.</summary>
    public int PerCodeDaily => runtime.Current.Limits.PerCodeDaily;

    /// <summary>The application just approved: its row is highlighted.</summary>
    [BindProperty(SupportsGet = true)]
    public long? Approved { get; set; }

    public async Task OnGetAsync()
    {
        var byId = (await codes.ListAsync()).ToDictionary(c => c.Id);
        Rows = (await applications.ListAsync()).Select(a =>
        {
            var code = a.CodeId is { } id && byId.TryGetValue(id, out var c) ? c : null;
            return new Row(a, code, a.Status == "approved" && code is not null ? InviteMessage.For(a.Name, code) : null);
        }).ToList();
    }

    /// <summary>
    /// Approving an already approved application (a resubmitted form, a second tab) makes no second code. The status
    /// change is one conditional update, so two tabs approving at once cannot both win: the loser disables the code it
    /// just made and the row shows the winner's.
    /// </summary>
    public async Task<IActionResult> OnPostApproveAsync(long id)
    {
        var a = (await applications.ListAsync()).FirstOrDefault(x => x.Id == id);
        if (a is null) return NotFound();
        if (a.Status != "approved" || a.CodeId is null)
        {
            var code = await codes.CreateAsync(a.Email, PerCodeDaily);
            if (a.Status != "approved")
            {
                if (!await applications.TryApproveAsync(a.Id, code.Id)) await codes.SetDisabledAsync(code.Id, true);
            }
            else
            {
                // Approved with no code (should not happen): keep the single-row change simple and give it this one.
                await applications.SetStatusAsync(a.Id, "approved", code.Id);
            }
        }
        return RedirectToPage(new { uid = Uid, approved = id });
    }

    /// <summary>Only a new application can be declined; an approved one keeps its code (disable it on Codes).</summary>
    public async Task<IActionResult> OnPostDeclineAsync(long id)
    {
        var a = (await applications.ListAsync()).FirstOrDefault(x => x.Id == id);
        if (a is null) return NotFound();
        if (a.Status == "new") await applications.SetStatusAsync(a.Id, "declined", null);
        return RedirectToPage(new { uid = Uid });
    }
}
