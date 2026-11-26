/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20231220

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261002
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add new Workspace projects and update AITokenMonitor icon

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.AI;

namespace Senparc.Xncf.AIKernel
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        /// <summary>Gets the AIKernel administration home URL.</summary>
        public string HomeUrl => "/Admin/AIKernel/Index";

        /// <summary>Gets the AIKernel administration menu entries.</summary>
        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
new AreaPageMenuItem(GetAreaHomeUrl(), AIKernelResource.Get("Area.Home", "首页"),"fa fa-laptop"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/AIVector/Index"), AIKernelResource.Get("Area.AIKernel.VectorDatabase", "向量数据库"),"fa fa-bookmark-o"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/Dashboard/Index"), AIKernelResource.Get("Area.AIKernel.Dashboard", "仪表盘"),"fa fa-dashboard"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/AITokenMonitor/Index"), AIKernelResource.Get("Area.AIKernel.TokenMonitor", "Token 监控"),"fa fa-tachometer"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/AIFineTuning/Index"), AIKernelResource.Get("FineTuning.Title", "本地模型微调"),"fa fa-cogs")
//Legacy comment translated to English.
};

        /// <summary>
        /// Registers AIKernel Razor page options and startup diagnostics.
        /// </summary>
        /// <param name="builder">The MVC builder to configure.</param>
        /// <param name="env">The hosting environment.</param>
        /// <returns>The configured MVC builder.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("AIKernel 启动", "完成 Area:Senparc.Xncf.AIKernel 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        /// <summary>Gets the absolute AIKernel path used for Razor runtime compilation.</summary>
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.AIKernel"));
        #endregion
    }
}
