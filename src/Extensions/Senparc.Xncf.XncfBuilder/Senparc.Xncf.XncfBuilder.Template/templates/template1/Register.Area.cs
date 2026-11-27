/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20200819

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

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

namespace Template_OrgName.Xncf.Template_XncfName
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        /// <summary>
        /// Gets the absolute administration URL used as the module home page.
        /// </summary>
        public string HomeUrl => "/Admin/Template_XncfName/Index";

        /// <summary>
        /// Gets the administration menu entries exposed by the template module.
        /// </summary>
        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), Template_XncfNameResource.Get("Area.Home"), "fa fa-laptop"),

                          new AreaPageMenuItem(GetAreaUrl($"/Admin/Template_XncfName/DatabaseSampleIndex"), Template_XncfNameResource.Get("Area.DatabaseListSample"), "fa fa-bookmark-o"),

                          new AreaPageMenuItem(GetAreaUrl($"/Admin/Template_XncfName/DatabaseSample"), Template_XncfNameResource.Get("Area.DatabaseSample"), "fa fa-bookmark-o")
        };

        /// <summary>
        /// Configures Razor Pages authorization hooks and writes the module registration log entry.
        /// </summary>
        /// <param name="builder">The MVC builder being configured by the host.</param>
        /// <param name="env">The hosting environment used for environment-specific configuration.</param>
        /// <returns>The same MVC builder after module configuration.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog(
                Template_XncfNameResource.Get("Area.StartupLog"),
                Template_XncfNameResource.Get("Area.RegisteredLog"));

            return builder;
        }

        #endregion

        #region Legacy section
        /// <summary>
        /// Gets the absolute path to the generated module library used for runtime compilation.
        /// </summary>
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Template_OrgName.Xncf.Template_XncfName"));
        #endregion
    }
}
