/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Workspace 运行时与远程编辑管理页面

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Areas.Workspace.Pages;

[AdminAuthorize("AdminSuperAdmin")]
public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly IWorkspaceStorage _storage;
    private readonly WorkspaceAgentProcessFactory _processFactory;

    public Index(
        Lazy<XncfModuleService> xncfModuleService,
        IWorkspaceStorage storage,
        WorkspaceAgentProcessFactory processFactory)
        : base(xncfModuleService)
    {
        _storage = storage;
        _processFactory = processFactory;
    }

    [BindProperty]
    public string NewWorkspaceName { get; set; } = string.Empty;

    public IReadOnlyList<WorkspaceDescriptor> Workspaces { get; private set; } = [];

    public WorkspaceRuntimeStatus RuntimeStatus => _processFactory.Status;

    public async Task OnGetAsync()
    {
        _ = XncfRegister;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var ownerId) || ownerId <= 0)
        {
            Workspaces = [];
            return;
        }

        Workspaces = (await _storage.ListWorkspacesAsync().ConfigureAwait(false))
            .Where(workspace => workspace.OwnerId == ownerId)
            .ToArray();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        _ = XncfRegister;
        if (string.IsNullOrWhiteSpace(NewWorkspaceName))
        {
            TempData["WorkspaceError"] = "请输入工作区名称。";
            return RedirectToPage("./Index", new { uid = Uid });
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var ownerId) || ownerId <= 0)
        {
            TempData["WorkspaceError"] = "当前管理员身份无效，请重新登录。";
            return RedirectToPage("./Index", new { uid = Uid });
        }

        await _storage.CreateAsync(
            new WorkspaceCreateRequest(ownerId, NewWorkspaceName.Trim())).ConfigureAwait(false);
        TempData["WorkspaceMessage"] = "工作区已创建，可在 VS Code 或 Cursor 中连接。";
        return RedirectToPage("./Index", new { uid = Uid });
    }
}
