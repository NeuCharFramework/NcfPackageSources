/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261125
    Change description: v0.4.4 Improved Sandbox setup, orchestration, and localized user feedback.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.Sandbox.Application.DTOs.Request;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using System.Globalization;

namespace Senparc.Xncf.Sandbox.Areas.Sandbox.Pages;

public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly SandboxOrchestrator _orchestrator;

    public Index(Lazy<XncfModuleService> xncfModuleService, SandboxOrchestrator orchestrator)
        : base(xncfModuleService)
    {
        _orchestrator = orchestrator;
    }

    public IReadOnlyList<SandboxSessionInfo> Sessions { get; private set; } = Array.Empty<SandboxSessionInfo>();

    public string DocsUrl { get; private set; } = SandboxDocsLinks.EnvironmentSetupZh;

    public async Task OnGetAsync()
    {
        DocsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name);
        Sessions = await _orchestrator.ListAsync().ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetStateAsync()
    {
        var sessions = await _orchestrator.ListAsync().ConfigureAwait(false);
        return new JsonResult(new
        {
            success = true,
            templates = _orchestrator.ListTemplates().Select(z => new
            {
                z.Key,
                z.DisplayName,
                z.Interactive,
                runtime = z.PreferredRuntime.ToString(),
                z.DefaultMemoryMb,
                defaultTtlMinutes = (int)z.DefaultTtl.TotalMinutes
            }),
            sessions
        });
    }

    public async Task<IActionResult> OnPostDestroyAsync([FromForm] string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _orchestrator.DestroyAsync(sessionId.Trim()).ConfigureAwait(false);
        }

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync([FromForm] string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _orchestrator.DeleteRecordAsync(sessionId.Trim()).ConfigureAwait(false);
        }

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync([FromForm] string[] sessionIds)
    {
        var result = await _orchestrator.DeleteRecordsAsync(sessionIds).ConfigureAwait(false);
        return new JsonResult(new
        {
            success = true,
            deleted = result.Deleted,
            skipped = result.Skipped
        });
    }

    public async Task<IActionResult> OnPostUpdateTtlAsync([FromForm] Sandbox_UpdateTtlRequest request)
    {
        var session = await _orchestrator.UpdateTtlAsync(
                request.SessionId,
                request.TtlMinutes,
                request.KeepAlive)
            .ConfigureAwait(false);

        return new JsonResult(new { success = true, session });
    }

    public async Task<IActionResult> OnPostUpdateAliasAsync([FromForm] string sessionId, [FromForm] string alias)
    {
        var session = await _orchestrator.UpdateAliasAsync(sessionId.Trim(), alias).ConfigureAwait(false);
        return new JsonResult(new { success = true, session });
    }
}
