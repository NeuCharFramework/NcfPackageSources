/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Analytics.cshtml.cs
    File description: Workflow analytics page adapter

    Modified by: Senparc - 20260829
    Change description: v0.3.0 add workflow analytics queries and Admin visualization

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.5.0.0 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using Senparc.Xncf.NeuCharWorkflow.Domain.Services;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

public class AnalyticsModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>
    /// Handles the initial analytics page request; data is loaded by the AJAX endpoint.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnGetAsync() => Task.CompletedTask;

    /// <summary>
    /// Returns owner-scoped workflow analytics with optional time, workflow, status,
    /// resource, keyword, and paging filters.
    /// </summary>
    /// <param name="from">Optional lower time-bound text.</param>
    /// <param name="to">Optional upper time-bound text.</param>
    /// <param name="workflowId">Optional workflow identifier.</param>
    /// <param name="status">Optional execution status.</param>
    /// <param name="resourceType">Optional resource type filter.</param>
    /// <param name="resourceProviderId">Optional resource-provider identifier.</param>
    /// <param name="resourceId">Optional resource identifier.</param>
    /// <param name="resourceName">Optional resource-name filter.</param>
    /// <param name="keyword">Optional free-text filter.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>An <see cref="OkObjectResult"/> containing the analytics result.</returns>
    public async Task<IActionResult> OnGetDataAsync(
        string? from = null,
        string? to = null,
        int? workflowId = null,
        string? status = null,
        string? resourceType = null,
        string? resourceProviderId = null,
        string? resourceId = null,
        string? resourceName = null,
        string? keyword = null,
        int page = 1,
        int pageSize = 25)
    {
        var result = await workflowAppService.GetAnalyticsAsync(
                CurrentAdminUserId,
                new WorkflowAnalyticsQuery(
                    from,
                    to,
                    workflowId,
                    status,
                    resourceType,
                    resourceProviderId,
                    resourceId,
                    resourceName,
                    keyword,
                    page,
                    pageSize),
                HttpContext.RequestAborted)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets the current administrator identifier used to scope analytics queries.
    /// </summary>
    private int CurrentAdminUserId => adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
}
