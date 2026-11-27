/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Tasks.cshtml.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20260822
    Change description: v0.2.0 improve workflow function calls, task control, and replay management

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
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>
/// Workflow task list page. Historical tasks come from execution logs, while
/// the run coordinator supplies live state for tasks still in progress.
/// </summary>
public class TasksModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>
    /// Handles the initial workflow-task page request; task data is loaded by the list endpoint.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnGetAsync() => Task.CompletedTask;

    /// <summary>
    /// Returns owner-scoped workflow tasks with optional cursor, workflow, status, and date filters.
    /// </summary>
    /// <param name="beforeExecutionLogId">Optional cursor for records before an execution-log identifier.</param>
    /// <param name="workflowId">Optional workflow identifier filter.</param>
    /// <param name="status">Optional task status filter.</param>
    /// <param name="from">Optional lower date-bound text.</param>
    /// <param name="to">Optional upper date-bound text.</param>
    /// <returns>An <see cref="OkObjectResult"/> containing the task list.</returns>
    public async Task<IActionResult> OnGetListAsync(
        int? beforeExecutionLogId = null,
        int? workflowId = null,
        string? status = null,
        string? from = null,
        string? to = null) =>
        Ok(await workflowAppService.GetTaskListAsync(
            CurrentAdminUserId,
            beforeExecutionLogId,
            workflowId,
            status,
            from,
            to,
            HttpContext.RequestAborted).ConfigureAwait(false));

    /// <summary>
    /// Returns the count and metadata of completed tasks eligible for cleanup.
    /// </summary>
    /// <returns>An <see cref="OkObjectResult"/> containing the cleanup preview.</returns>
    public async Task<IActionResult> OnGetCleanupPreviewAsync() =>
        Ok(await workflowAppService.PreviewTaskCleanupAsync(CurrentAdminUserId, HttpContext.RequestAborted)
            .ConfigureAwait(false));

    /// <summary>
    /// Deletes completed workflow tasks older than the requested cutoff for the current administrator.
    /// </summary>
    /// <param name="request">The optional cleanup cutoff.</param>
    /// <returns>An action result containing cleanup details or a bad-request message.</returns>
    public async Task<IActionResult> OnPostCleanupAsync([FromBody] TaskCleanupRequest request)
    {
        try
        {
            return Ok(await workflowAppService.CleanupCompletedTasksAsync(
                CurrentAdminUserId,
                request?.Cutoff ?? DateTime.MinValue,
                HttpContext.RequestAborted).ConfigureAwait(false));
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Requests cancellation of an active workflow run or execution log.
    /// </summary>
    /// <param name="request">The run or execution-log identifier to abort.</param>
    /// <returns>A success result or a conflict/bad-request response.</returns>
    public async Task<IActionResult> OnPostAbortAsync([FromBody] AbortWorkflowRunRequest request)
    {
        try
        {
            await workflowAppService.AbortRunAsync(
                request?.RunId,
                request?.ExecutionLogId,
                CurrentAdminUserId,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(new { success = true });
        }
        catch (WorkflowConflictException ex)
        {
            return StatusCode(409, ex.Message);
        }
        catch (WorkflowInputException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private int CurrentAdminUserId => adminWorkContextProvider.GetAdminWorkContext().AdminUserId;

    public sealed class AbortWorkflowRunRequest
    {
        public Guid? RunId { get; set; }
        public int? ExecutionLogId { get; set; }
    }

    public sealed class TaskCleanupRequest
    {
        public DateTime? Cutoff { get; set; }
    }
}
