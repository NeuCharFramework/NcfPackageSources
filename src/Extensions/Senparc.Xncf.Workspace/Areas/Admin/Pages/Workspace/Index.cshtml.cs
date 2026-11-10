/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Index.cshtml.cs
    文件功能描述：Workspace 运行时与远程编辑管理页面

    创建标识：Senparc - 20261024

    修改标识：Senparc - 20261103
    修改描述：v0.2.2 implement connection manager and workspace file system provider

    修改标识：Senparc - 20261109
    修改描述：展示按所有者隔离的用量，并允许在权限范围内清空工作区内容

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Workspace.Domain.Services;

namespace Senparc.Xncf.Workspace.Areas.Workspace.Pages;

[AdminAuthorize("AdminSuperAdmin")]
public sealed class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Default
    };
    private readonly IWorkspaceCatalog _catalog;
    private readonly ILogger<Index> _logger;

    public Index(
        Lazy<XncfModuleService> xncfModuleService,
        IWorkspaceCatalog catalog,
        ILogger<Index> logger)
        : base(xncfModuleService)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [BindProperty]
    public string NewWorkspaceName { get; set; } = string.Empty;

    public WorkspaceUsageReport Report { get; private set; } =
        WorkspaceUsageReport.Empty(new WorkspaceActor(0, false), 0);

    public string ClientReportJson { get; private set; } = "{}";

    public async Task OnGetAsync()
    {
        _ = XncfRegister;
        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            Report = WorkspaceUsageReport.Empty(new WorkspaceActor(0, false), 0);
            ClientReportJson = SerializeReport(Report);
            TempData["WorkspaceError"] = "当前管理员身份无效，请重新登录。";
            return;
        }

        try
        {
            Report = await _catalog.GetReportAsync(actor).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "读取工作区用量失败: ActorId={ActorId}", actor.UserId);
            Report = WorkspaceUsageReport.Empty(actor, 0);
            TempData["WorkspaceError"] = "读取工作区用量失败，请稍后重试。";
        }

        ClientReportJson = SerializeReport(Report);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        _ = XncfRegister;
        if (string.IsNullOrWhiteSpace(NewWorkspaceName))
        {
            TempData["WorkspaceError"] = "请输入工作区名称。";
            return RedirectToPage("./Index", new { uid = Uid });
        }

        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            TempData["WorkspaceError"] = "当前管理员身份无效，请重新登录。";
            return RedirectToPage("./Index", new { uid = Uid });
        }

        await _catalog.CreateOwnAsync(actor, NewWorkspaceName.Trim()).ConfigureAwait(false);
        TempData["WorkspaceMessage"] = "工作区已创建，可在 VS Code 或 Cursor 中连接。";
        return RedirectToPage("./Index", new { uid = Uid });
    }

    public async Task<IActionResult> OnPostClearAsync(string workspaceId)
    {
        _ = XncfRegister;
        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            TempData["WorkspaceError"] = "当前管理员身份无效，请重新登录。";
            return RedirectToPage("./Index", new { uid = Uid });
        }

        try
        {
            var result = await _catalog.ClearContentAsync(actor, workspaceId).ConfigureAwait(false);
            TempData["WorkspaceMessage"] = result.ClearedOtherOwner
                ? $"已清空其他管理员 #{result.OwnerId} 的工作区「{result.Name}」，内容已恢复为新建时的空目录。"
                : $"已清空工作区「{result.Name}」，内容已恢复为新建时的空目录。";
        }
        catch (ArgumentException)
        {
            TempData["WorkspaceError"] = "请选择要清空的工作区。";
        }
        catch (DirectoryNotFoundException)
        {
            TempData["WorkspaceError"] = "工作区不存在，或不在当前账号的管理范围内。";
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "清空工作区内容失败: ActorId={ActorId}, WorkspaceId={WorkspaceId}",
                actor.UserId,
                workspaceId);
            TempData["WorkspaceError"] = "清空工作区失败，请稍后重试。";
        }

        return RedirectToPage("./Index", new { uid = Uid });
    }

    private static string SerializeReport(WorkspaceUsageReport report) =>
        JsonSerializer.Serialize(new
        {
            isSuperAdmin = report.IsSuperAdmin,
            actorId = report.ActorId,
            quotaBytes = report.QuotaBytes,
            ownSummary = report.OwnSummary,
            otherSummary = report.OtherSummary,
            visibleSummary = report.VisibleSummary,
            ownWorkspaces = report.OwnWorkspaces.Select(ToClientItem),
            otherWorkspaces = report.OtherWorkspaces.Select(ToClientItem)
        }, JsonOptions);

    private static object ToClientItem(WorkspaceUsageItem item) => new
    {
        id = item.Id,
        name = item.Name,
        ownerId = item.OwnerId,
        ownerType = item.OwnerType,
        createdUtc = item.CreatedUtc,
        lastAccessedUtc = item.LastAccessedUtc,
        scope = item.Scope == WorkspaceOwnershipScope.Own ? "Own" : "Other",
        fileCount = item.Usage.FileCount,
        directoryCount = item.Usage.DirectoryCount,
        usedBytes = item.Usage.UsedBytes,
        quotaBytes = item.Usage.QuotaBytes
    };
}
