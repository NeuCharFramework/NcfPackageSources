/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Area.cs
    文件功能描述：DesktopBridge 管理页面注册

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.DesktopBridge;

public sealed partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    public string HomeUrl => "/Admin/DesktopBridge/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), "设备配对与会话", "fa fa-desktop")
    ];

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        return builder.AddRazorPagesOptions(_ => { });
    }

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.DesktopBridge"));
}

