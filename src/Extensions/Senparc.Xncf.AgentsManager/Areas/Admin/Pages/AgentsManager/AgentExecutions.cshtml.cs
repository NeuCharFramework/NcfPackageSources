/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutions.cshtml.cs
    文件功能描述：独立 Agent 执行管理页模型

    创建标识：Senparc - 20260822
    修改描述：v0.16.0 支持独立 Agent 执行任务列表和详情

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增加独立 Agent 执行任务管理页


----------------------------------------------------------------*/

using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Service;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Areas.AgentsManager.Pages;

public sealed class AgentExecutionsModel : AdminXncfModulePageModelBase
{
    public AgentExecutionsModel(Lazy<XncfModuleService> xncfModuleService)
        : base(xncfModuleService)
    {
    }

    public Task OnGetAsync() => Task.CompletedTask;
}
