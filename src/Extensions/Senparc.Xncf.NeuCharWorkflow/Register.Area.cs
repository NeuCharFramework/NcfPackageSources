/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Workflow orchestration, replay, Webhook, and parallel execution

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 enhance Workflow orchestration, replay, Webhook, and parallel execution

    Modified by: Senparc - 20261119
    Change description: v0.4.7 Localized Workflow designer and runtime surfaces

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
    /// <summary>Gets the Workflow administration home URL.</summary>
    public string HomeUrl => "/Admin/NeuCharWorkflow/Index";

    /// <summary>Gets the Workflow administration menu entries.</summary>
    public List<AreaPageMenuItem> AreaPageMenuItems => new()
    {
        new AreaPageMenuItem(GetAreaHomeUrl(), NeuCharWorkflowResource.Get("Area.Workflow", "Workflow"), "fa fa-random"),
        new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharWorkflow/Tasks"), NeuCharWorkflowResource.Get("Area.Tasks", "任务列表"), "fa fa-list-alt")
    };

    /// <summary>
    /// Registers Workflow Razor Pages and applies the AdminOnly authorization policy.
    /// </summary>
    /// <param name="builder">The MVC builder being configured.</param>
    /// <param name="env">The current host environment.</param>
    /// <returns>The configured MVC builder.</returns>
    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        builder.AddRazorPagesOptions(options =>
        {
            // All module pages require an authenticated administrator with AdminMemberClaim.
            options.Conventions.AuthorizeAreaFolder("Admin", "/NeuCharWorkflow", NcfAuthorizationPolicyNames.AdminOnly);
        });

        SenparcTrace.SendCustomLog("NeuChar Workflow started", "Registered the standalone XNCF Area and AdminOnly authorization.");
        return builder;
    }

    /// <summary>Gets the absolute Workflow module path used for runtime compilation.</summary>
    public string LibraryPath => Path.GetFullPath(Path.Combine(
        SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.NeuCharWorkflow"));
}
