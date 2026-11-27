/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.Sandbox;

public partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    public string HomeUrl => "/Admin/Sandbox/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems => new()
    {
        new AreaPageMenuItem(GetAreaHomeUrl(), SandboxResource.Get("Area.Menu.Home", "沙箱面板"), "fa fa-laptop"),
        new AreaPageMenuItem(GetAreaUrl("/Admin/Sandbox/Setup"), SandboxResource.Get("Area.Menu.Setup", "环境准备"), "fa fa-wrench")
    };

    /// <summary>
    /// Configures Razor Page authorization hooks for the Sandbox area.
    /// </summary>
    /// <param name="builder">The MVC builder receiving the area configuration.</param>
    /// <param name="env">The hosting environment used by MVC configuration.</param>
    /// <returns>The configured MVC builder.</returns>
    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        builder.AddRazorPagesOptions(_ =>
        {
            //Legacy comment translated to English.
        });

        SenparcTrace.SendCustomLog("Sandbox 启动", "完成 Area:Senparc.Xncf.Sandbox 注册");
        return builder;
    }

    public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Sandbox"));
}
