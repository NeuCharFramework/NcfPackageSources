/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Area.cs
    文件功能描述：模块的后台页面注册（菜单、区域与 Razor 运行时编译）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 隧道守卫已移除：注释改为后台页面只受 NCF 管理员登录保护

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 首页改为设置页；菜单依次为设置、邀请码、申请、运行记录，文字取自资源文件

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;

namespace Senparc.Xncf.NeuCharBoxLive
{
    /// <summary>
    /// The admin pages, inside the NCF admin area: the NCF admin login protects them, like every other module's pages
    /// (the module's own pipeline branch covers /live/api only).
    /// </summary>
    public partial class Register : IAreaRegister, //注册 XNCF 页面接口
                                    IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/NeuCharBoxLive/Settings";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
            new AreaPageMenuItem(GetAreaHomeUrl(), NeuCharBoxLiveResource.Get("Area.NeuCharBoxLive.Settings", "Settings"), "fa fa-sliders-h"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharBoxLive/Codes"), NeuCharBoxLiveResource.Get("Area.NeuCharBoxLive.Codes", "Codes"), "fa fa-key"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharBoxLive/Applications"), NeuCharBoxLiveResource.Get("Area.NeuCharBoxLive.Applications", "Applications"), "fa fa-inbox"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharBoxLive/Runs"), NeuCharBoxLiveResource.Get("Area.NeuCharBoxLive.Runs", "Runs"), "fa fa-list"),
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限（页面基类已要求管理员登录）
            });

            SenparcTrace.SendCustomLog("NeuCharBoxLive 启动", "完成 Area:Senparc.Xncf.NeuCharBoxLive 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.NeuCharBoxLive"));
        #endregion
    }
}
