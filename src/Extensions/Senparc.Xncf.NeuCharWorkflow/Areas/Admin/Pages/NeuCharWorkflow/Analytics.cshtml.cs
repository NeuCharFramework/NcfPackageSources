/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Analytics.cshtml.cs
    文件功能描述：Workflow 统计分析页面适配器

    修改标识：Senparc - 20260829
    修改描述：v0.3.0 新增工作流分析查询与管理端可视化

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
    public Task OnGetAsync() => Task.CompletedTask;

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

    private int CurrentAdminUserId => adminWorkContextProvider.GetAdminWorkContext().AdminUserId;
}
