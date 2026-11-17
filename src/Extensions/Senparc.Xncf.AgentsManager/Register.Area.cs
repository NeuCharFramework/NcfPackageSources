/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext add the standardized file header

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 integrate AgentsManager with unified localization and refine UI copy

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

namespace Senparc.Xncf.AgentsManager
{
    public partial class Register : IAreaRegister, // Register the XNCF pages when needed.
                                    IXncfRazorRuntimeCompilation  // Enable Razor Page runtime compilation.
    {
        #region Legacy section

        public string HomeUrl => "/Admin/AgentsManager/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                    new AreaPageMenuItem(GetAreaHomeUrl(), AgentsManagerResource.Get("Area.Home", "首页"),"fa fa-laptop"),
			 		//new AreaPageMenuItem(GetAreaUrl($"/Admin/AgentsManager/DatabaseSample"), "Database example", "fa fa-bookmark-o")
			};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                // Configure page permissions here when required.
            });

            SenparcTrace.SendCustomLog("AgentsManager startup", "Area registration completed: Senparc.Xncf.AgentsManager");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.AgentsManager"));
        #endregion
    }
}
