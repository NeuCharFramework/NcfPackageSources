/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Workspace runtime and remote editing administration page

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261109
    Change description: show owner-isolated usage and allow content clearing within authorization boundaries

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.2.9.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Service;
using Senparc.Xncf.Workspace;
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

    /// <summary>
    /// Initializes the Workspace administration page with catalog and logging dependencies.
    /// </summary>
    /// <param name="xncfModuleService">The lazy module service used by the base page model.</param>
    /// <param name="catalog">The workspace catalog used for usage and content operations.</param>
    /// <param name="logger">The page logger.</param>
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

    /// <summary>
    /// Loads the current administrator's visible Workspace usage report and serializes
    /// it for the client-side administration page.
    /// </summary>
    public async Task OnGetAsync()
    {
        _ = XncfRegister;
        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            Report = WorkspaceUsageReport.Empty(new WorkspaceActor(0, false), 0);
            ClientReportJson = SerializeReport(Report);
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.IdentityInvalid", "The current administrator identity is invalid. Please sign in again.");
            return;
        }

        try
        {
            Report = await _catalog.GetReportAsync(actor).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to read Workspace usage: ActorId={ActorId}", actor.UserId);
            Report = WorkspaceUsageReport.Empty(actor, 0);
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.UsageReadFailed", "Failed to read Workspace usage. Please retry later.");
        }

        ClientReportJson = SerializeReport(Report);
    }

    /// <summary>
    /// Creates a Workspace owned by the current administrator after identity validation.
    /// </summary>
    /// <returns>A redirect to the Workspace page with a success or error message.</returns>
    /// <exception cref="ArgumentException">Propagated when the catalog rejects the workspace name.</exception>
    public async Task<IActionResult> OnPostCreateAsync()
    {
        _ = XncfRegister;
        if (string.IsNullOrWhiteSpace(NewWorkspaceName))
        {
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.NameRequired", "Enter a Workspace name.");
            return RedirectToPage("./Index", new { uid = Uid });
        }

        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.IdentityInvalid", "The current administrator identity is invalid. Please sign in again.");
            return RedirectToPage("./Index", new { uid = Uid });
        }

        await _catalog.CreateOwnAsync(actor, NewWorkspaceName.Trim()).ConfigureAwait(false);
        TempData["WorkspaceMessage"] = WorkspaceResource.Get("Server.Created", "Workspace created. Connect with VS Code or Cursor.");
        return RedirectToPage("./Index", new { uid = Uid });
    }

    /// <summary>
    /// Clears the selected Workspace content within the catalog's ownership and authorization rules.
    /// </summary>
    /// <param name="workspaceId">The Workspace identifier to clear.</param>
    /// <returns>A redirect to the Workspace page with the operation result.</returns>
    public async Task<IActionResult> OnPostClearAsync(string workspaceId)
    {
        _ = XncfRegister;
        if (!WorkspaceActors.TryResolve(User, out var actor))
        {
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.IdentityInvalid", "The current administrator identity is invalid. Please sign in again.");
            return RedirectToPage("./Index", new { uid = Uid });
        }

        try
        {
            var result = await _catalog.ClearContentAsync(actor, workspaceId).ConfigureAwait(false);
            TempData["WorkspaceMessage"] = result.ClearedOtherOwner
                ? WorkspaceResource.Format("Server.ClearedOther", "Cleared administrator #{0}'s Workspace \"{1}\". Its content was reset to the empty initial layout.", result.OwnerId, result.Name)
                : WorkspaceResource.Format("Server.ClearedOwn", "Cleared Workspace \"{0}\". Its content was reset to the empty initial layout.", result.Name);
        }
        catch (ArgumentException)
        {
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.SelectionRequired", "Select a Workspace to clear.");
        }
        catch (DirectoryNotFoundException)
        {
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.NotFound", "The Workspace does not exist or is outside the current administrator's scope.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to clear Workspace content: ActorId={ActorId}, WorkspaceId={WorkspaceId}",
                actor.UserId,
                workspaceId);
            TempData["WorkspaceError"] = WorkspaceResource.Get("Server.ClearFailed", "Failed to clear the Workspace. Please retry later.");
        }

        return RedirectToPage("./Index", new { uid = Uid });
    }

    /// <summary>
    /// Projects a server usage report into the JSON contract consumed by the administration page.
    /// </summary>
    /// <param name="report">The server-side usage report.</param>
    /// <returns>Serialized client-report JSON.</returns>
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

    /// <summary>
    /// Projects one workspace usage item into its client-facing JSON shape.
    /// </summary>
    /// <param name="item">The workspace usage item.</param>
    /// <returns>An anonymous client-report item.</returns>
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
