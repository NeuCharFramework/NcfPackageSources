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

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

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

    /// <summary>
    /// Initializes the Sandbox administration page.
    /// </summary>
    /// <param name="xncfModuleService">The lazy XNCF module service.</param>
    /// <param name="orchestrator">The Sandbox session orchestrator.</param>
    public Index(Lazy<XncfModuleService> xncfModuleService, SandboxOrchestrator orchestrator)
        : base(xncfModuleService)
    {
        _orchestrator = orchestrator;
    }

    public IReadOnlyList<SandboxSessionInfo> Sessions { get; private set; } = Array.Empty<SandboxSessionInfo>();

    public string DocsUrl { get; private set; } = SandboxDocsLinks.EnvironmentSetupZh;

    /// <summary>
    /// Loads localized documentation URL and current sessions.
    /// </summary>
    /// <returns>A task that completes after page state is loaded.</returns>
    public async Task OnGetAsync()
    {
        DocsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name);
        Sessions = await _orchestrator.ListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns templates and sessions for asynchronous administration updates.
    /// </summary>
    /// <returns>An HTTP 200 JSON state result.</returns>
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

    /// <summary>
    /// Stops a Sandbox session when an identifier is supplied.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>An HTTP 200 JSON result.</returns>
    public async Task<IActionResult> OnPostDestroyAsync([FromForm] string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _orchestrator.DestroyAsync(sessionId.Trim()).ConfigureAwait(false);
        }

        return new JsonResult(new { success = true });
    }

    /// <summary>
    /// Deletes a terminal Sandbox session record when an identifier is supplied.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>An HTTP 200 JSON result.</returns>
    public async Task<IActionResult> OnPostDeleteAsync([FromForm] string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _orchestrator.DeleteRecordAsync(sessionId.Trim()).ConfigureAwait(false);
        }

        return new JsonResult(new { success = true });
    }

    /// <summary>
    /// Deletes multiple Sandbox session records and reports deleted/skipped counts.
    /// </summary>
    /// <param name="sessionIds">The session identifiers to process.</param>
    /// <returns>An HTTP 200 JSON result.</returns>
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

    /// <summary>
    /// Updates one session's retention or keep-alive settings.
    /// </summary>
    /// <param name="request">The session TTL request.</param>
    /// <returns>An HTTP 200 JSON result containing the updated session.</returns>
    public async Task<IActionResult> OnPostUpdateTtlAsync([FromForm] Sandbox_UpdateTtlRequest request)
    {
        var session = await _orchestrator.UpdateTtlAsync(
                request.SessionId,
                request.TtlMinutes,
                request.KeepAlive)
            .ConfigureAwait(false);

        return new JsonResult(new { success = true, session });
    }

    /// <summary>
    /// Updates one session's display alias.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="alias">The new display alias.</param>
    /// <returns>An HTTP 200 JSON result containing the updated session.</returns>
    public async Task<IActionResult> OnPostUpdateAliasAsync([FromForm] string sessionId, [FromForm] string alias)
    {
        var session = await _orchestrator.UpdateAliasAsync(sessionId.Trim(), alias).ConfigureAwait(false);
        return new JsonResult(new { success = true, session });
    }
}
