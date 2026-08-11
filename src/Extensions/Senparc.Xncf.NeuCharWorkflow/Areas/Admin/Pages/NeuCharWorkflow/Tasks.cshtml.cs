using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>
/// Workflow 任务列表页面。历史任务来自执行日志，仍在执行的任务由运行协调器补充实时状态。
/// </summary>
public class TasksModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    public Task OnGetAsync() => Task.CompletedTask;

    public async Task<IActionResult> OnGetListAsync(int? beforeExecutionLogId = null) =>
        Ok(await workflowAppService.GetTaskListAsync(
            CurrentAdminUserId,
            beforeExecutionLogId,
            HttpContext.RequestAborted).ConfigureAwait(false));

    public async Task<IActionResult> OnGetCleanupPreviewAsync() =>
        Ok(await workflowAppService.PreviewTaskCleanupAsync(CurrentAdminUserId, HttpContext.RequestAborted)
            .ConfigureAwait(false));

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

    public IActionResult OnPostAbort([FromBody] AbortWorkflowRunRequest request)
    {
        try
        {
            workflowAppService.AbortRun(request?.RunId ?? Guid.Empty, CurrentAdminUserId);
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
        public Guid RunId { get; set; }
    }

    public sealed class TaskCleanupRequest
    {
        public DateTime? Cutoff { get; set; }
    }
}
