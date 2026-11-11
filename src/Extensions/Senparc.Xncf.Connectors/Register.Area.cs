/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Area.cs
    文件功能描述：注册连接器模块的后台 Area 与菜单
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 注册连接器后台菜单

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.Connectors;

public sealed partial class Register : IAreaRegister
{
    public string HomeUrl => "/Admin/Connectors/Index";

    public List<AreaPageMenuItem> AreaPageMenuItems =>
    [
        new AreaPageMenuItem(GetAreaHomeUrl(), ConnectorsResource.Get("Area.Connectors.Config", "连接器配置"), "fa fa-plug")
    ];

    public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        => builder.AddRazorPagesOptions(_ => { });

    public string LibraryPath => Path.GetFullPath(
        Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Connectors"));
}
