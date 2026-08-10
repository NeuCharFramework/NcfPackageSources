using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
using System.Threading.Tasks;

namespace Senparc.Xncf.NeuCharWorkflow.Areas.NeuCharWorkflow.Pages;

/// <summary>已完成任务的只读回看页；不加载编辑器，也不会修改历史运行数据。</summary>
public class ReplayModel(
    Lazy<XncfModuleService> xncfModuleService,
    NeuCharWorkflowAppService workflowAppService,
    IAdminWorkContextProvider adminWorkContextProvider) : AdminXncfModulePageModelBase(xncfModuleService)
{
    public Task OnGetAsync() => Task.CompletedTask;

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
