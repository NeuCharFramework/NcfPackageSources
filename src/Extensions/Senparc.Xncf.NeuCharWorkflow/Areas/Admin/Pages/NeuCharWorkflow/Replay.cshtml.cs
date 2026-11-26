/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Replay.cshtml.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260811

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>Read-only replay page for completed tasks. It does not load the
/// editor or modify historical run data.</summary>
public class ReplayModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    /// <summary>
    /// Handles the initial read-only replay page request.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnGetAsync() => Task.CompletedTask;

    /// <summary>
    /// Loads one completed execution log for read-only replay.
    /// </summary>
    /// <param name="executionLogId">The execution-log identifier to replay.</param>
    /// <returns>The replay payload, not-found response, or validation/conflict response.</returns>
    public async Task<IActionResult> OnGetDataAsync(int executionLogId)
    {
        try
        {
            var replay = await workflowAppService.GetReplayAsync(
                executionLogId,
                CurrentAdminUserId,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return replay == null ? NotFound() : Ok(replay);
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

    /// <summary>
    /// Copies a completed replay into a new workflow draft for the current administrator.
    /// </summary>
    /// <param name="request">The execution-log identifier to copy.</param>
    /// <returns>The copied workflow or a not-found/conflict/validation response.</returns>
    public async Task<IActionResult> OnPostCopyAsync([FromBody] ReplayCopyRequest request)
    {
        try
        {
            var workflow = await workflowAppService.CopyReplayAsDraftAsync(
                request?.ExecutionLogId ?? 0,
                CurrentAdminUserId,
                HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(workflow);
        }
        catch (WorkflowNotFoundException)
        {
            return NotFound();
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

    public sealed class ReplayCopyRequest
    {
        public int ExecutionLogId { get; set; }
    }
}
