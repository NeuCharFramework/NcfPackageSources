/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Area.cs
    文件功能描述：增强工作流编排、回放、Webhook 与并行执行能力


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.NeuCharWorkflow;

public partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    public string HomeUrl => "/Admin/NeuCharWorkflow/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems => new()
    {
        new AreaPageMenuItem(GetAreaHomeUrl(), NeuCharWorkflowResource.Get("Area.Workflow", "Workflow"), "fa fa-random"),
        new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharWorkflow/Tasks"), NeuCharWorkflowResource.Get("Area.Tasks", "任务列表"), "fa fa-list-alt")
    };

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        builder.AddRazorPagesOptions(options =>
        {
            // 所有模块页面只允许具有 AdminMemberClaim 的已登录管理员访问。
            options.Conventions.AuthorizeAreaFolder("Admin", "/NeuCharWorkflow", NcfAuthorizationPolicyNames.AdminOnly);
        });

        SenparcTrace.SendCustomLog("NeuChar Workflow 启动", "完成独立 XNCF Area 注册与 AdminOnly 授权。");
        return builder;
    }

    public string LibraryPath => Path.GetFullPath(Path.Combine(
        SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.NeuCharWorkflow"));
}
