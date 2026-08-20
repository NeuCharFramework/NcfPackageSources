/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.Area.cs
    文件功能描述：XncfBuilder 管理页面注册

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.XncfBuilder
{
    public partial class Register : IAreaRegister, IXncfRazorRuntimeCompilation
    {
        public string HomeUrl => "/Admin/XncfBuilder/PreviewMonitor";

        public List<AreaPageMenuItem> AreaPageMenuItems => new()
        {
            new AreaPageMenuItem(
                GetAreaHomeUrl(),
                XncfBuilderResource.Get("XncfBuilder.Preview.Monitor", "XNCF 预览监控"),
                "fa fa-dashboard")
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            return builder.AddRazorPagesOptions(_ => { });
        }

        public string LibraryPath => Path.GetFullPath(
            Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.XncfBuilder"));
    }
}
