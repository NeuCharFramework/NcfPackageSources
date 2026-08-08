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

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        builder.AddRazorPagesOptions(_ =>
        {
            // 页面级权限可按角色继续收紧
        });

        SenparcTrace.SendCustomLog("Sandbox 启动", "完成 Area:Senparc.Xncf.Sandbox 注册");
        return builder;
    }

    public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Sandbox"));
}
