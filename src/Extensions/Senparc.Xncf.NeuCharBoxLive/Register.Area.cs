/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Area.cs
    文件功能描述：NeuCharBox Live 模块：Register.Area


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

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
    /// The admin pages, inside the NCF admin area: they need the admin login, and the tunnel guard keeps every path but
    /// /live/api unreachable from outside, so they work on localhost only.
    /// </summary>
    public partial class Register : IAreaRegister, //注册 XNCF 页面接口
                                    IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/NeuCharBoxLive/Applications";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
            new AreaPageMenuItem(GetAreaHomeUrl(), "Applications", "fa fa-inbox"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharBoxLive/Codes"), "Codes", "fa fa-key"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/NeuCharBoxLive/Runs"), "Runs", "fa fa-list"),
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
