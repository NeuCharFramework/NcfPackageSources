/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260801

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Areas.DesktopBridge.Pages;

public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly DesktopBridgeCredentialStore _credentialStore;

    /// <summary>
    /// Initializes the desktop bridge administration page.
    /// </summary>
    /// <param name="xncfModuleService">The lazy XNCF module service.</param>
    /// <param name="credentialStore">The desktop pairing/session store.</param>
    public Index(
        Lazy<XncfModuleService> xncfModuleService,
        DesktopBridgeCredentialStore credentialStore)
        : base(xncfModuleService)
    {
        _credentialStore = credentialStore;
    }

    public IReadOnlyList<DesktopBridgePendingPairingView> PendingPairings { get; private set; } = [];

    public IReadOnlyList<DesktopBridgeSessionView> Sessions { get; private set; } = [];

    /// <summary>
    /// Loads pending pairings and active sessions for the administrator.
    /// </summary>
    public void OnGet()
    {
        LoadState();
    }

    /// <summary>
    /// Approves one pending desktop pairing request.
    /// </summary>
    /// <param name="requestId">The pairing request identifier.</param>
    /// <returns>A redirect back to the desktop bridge page.</returns>
    public IActionResult OnPostApprove(Guid requestId)
    {
        var approvedBy = User.Identity?.Name;
        TempData["DesktopBridgeMessage"] = _credentialStore.Approve(requestId, approvedBy)
            ? "已批准配对。会话令牌仅会交付给发起请求的桌面端。"
            : "配对请求已过期、已处理，或活动会话已达到上限。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    /// <summary>
    /// Denies one pending desktop pairing request.
    /// </summary>
    /// <param name="requestId">The pairing request identifier.</param>
    /// <returns>A redirect back to the desktop bridge page.</returns>
    public IActionResult OnPostDeny(Guid requestId)
    {
        TempData["DesktopBridgeMessage"] = _credentialStore.Deny(requestId)
            ? "已拒绝配对请求。"
            : "配对请求已过期或已处理。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    /// <summary>
    /// Revokes one active desktop session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A redirect back to the desktop bridge page.</returns>
    public IActionResult OnPostRevoke(Guid sessionId)
    {
        TempData["DesktopBridgeMessage"] = _credentialStore.Revoke(sessionId)
            ? "会话已撤销，桌面端已收到断开信号并将退出当前网站状态，应用保持打开。"
            : "会话不存在或已经失效。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    /// <summary>
    /// Refreshes the page's pending pairing and active-session projections.
    /// </summary>
    private void LoadState()
    {
        _ = XncfRegister;
        PendingPairings = _credentialStore.GetPendingPairings();
        Sessions = _credentialStore.GetSessions();
    }
}
