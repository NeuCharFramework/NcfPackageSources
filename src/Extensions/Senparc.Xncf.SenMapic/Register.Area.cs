/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

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

namespace Senparc.Xncf.SenMapic
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        public string HomeUrl => "/Admin/SenMapic/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), SenMapicResource.Get("Area.Home", "Home"),"fa fa-laptop"),
                         new AreaPageMenuItem(GetAreaUrl($"/Admin/SenMapic/Index"),"SenMapic","fa fa-file"),
			 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/SenMapic/DatabaseSample"), SenMapicResource.Get("Area.DatabaseSample", "Database operations sample"),"fa fa-bookmark-o")
			 		};

        /// <summary>
        /// Registers SenMapic Razor page options and startup diagnostics.
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

            SenparcTrace.SendCustomLog("SenMapic startup", "Completed Area:Senparc.Xncf.SenMapic registration");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.SenMapic"));
        #endregion
    }
}
