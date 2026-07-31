/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：DesktopBridge 配对审批和会话撤销后台

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Areas.DesktopBridge.Pages;

public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly DesktopBridgeCredentialStore _credentialStore;

    public Index(
        Lazy<XncfModuleService> xncfModuleService,
        DesktopBridgeCredentialStore credentialStore)
        : base(xncfModuleService)
    {
        _credentialStore = credentialStore;
    }

    public IReadOnlyList<DesktopBridgePendingPairingView> PendingPairings { get; private set; } = [];

    public IReadOnlyList<DesktopBridgeSessionView> Sessions { get; private set; } = [];

    public void OnGet()
    {
        LoadState();
    }

    public IActionResult OnPostApprove(Guid requestId)
    {
        var approvedBy = User.Identity?.Name;
        TempData["DesktopBridgeMessage"] = _credentialStore.Approve(requestId, approvedBy)
            ? "已批准配对。会话令牌仅会交付给发起请求的桌面端。"
            : "配对请求已过期、已处理，或活动会话已达到上限。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    public IActionResult OnPostDeny(Guid requestId)
    {
        TempData["DesktopBridgeMessage"] = _credentialStore.Deny(requestId)
            ? "已拒绝配对请求。"
            : "配对请求已过期或已处理。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    public IActionResult OnPostRevoke(Guid sessionId)
    {
        TempData["DesktopBridgeMessage"] = _credentialStore.Revoke(sessionId)
            ? "会话已撤销，桌面端已收到断开信号并将退出当前网站状态，应用保持打开。"
            : "会话不存在或已经失效。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    private void LoadState()
    {
        _ = XncfRegister;
        PendingPairings = _credentialStore.GetPendingPairings();
        Sessions = _credentialStore.GetSessions();
    }
}
