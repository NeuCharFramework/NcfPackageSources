using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Xncf.NeuCharWorkflow.Application.AppServices;
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

    public async Task<IActionResult> OnGetListAsync() =>
        Ok(await workflowAppService.GetTaskListAsync(CurrentAdminUserId, HttpContext.RequestAborted).ConfigureAwait(false));

    private int CurrentAdminUserId => adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
}
