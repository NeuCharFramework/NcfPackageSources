/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20240311

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.13.0-preview3 Updated module behavior and related support.

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
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.AIAgentsHub
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        public string HomeUrl => "/Admin/AIAgentsHub/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), AIAgentsHubResource.Get("Area.Home", "首页"),"fa fa-laptop"),
			 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/AIAgentsHub/DatabaseSample"), AIAgentsHubResource.Get("Area.DatabaseSample", "数据库操作示例"),"fa fa-bookmark-o")
			 		};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("AIAgentsHub 启动", "完成 Area:Senparc.Xncf.AIAgentsHub 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.AIAgentsHub"));
        #endregion
    }
}
