/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Area.cs
    文件功能描述：Workspace 管理页面注册

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.Workspace;

public sealed partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
{
    public string HomeUrl => "/Admin/Workspace/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), "工作区与远程编辑", "fa fa-code")
    ];

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
    {
        return builder.AddRazorPagesOptions(_ => { });
    }

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Workspace"));
}
