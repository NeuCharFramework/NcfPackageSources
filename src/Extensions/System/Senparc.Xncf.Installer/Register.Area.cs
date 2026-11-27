/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Senparc.Xncf.Installer related implementation

    Created by: Senparc - 20200819

    Modified by: Senparc - 20261127
    Change description: v0.6.0.1 Improve source documentation, headers, and module metadata consistency.

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
using System.Globalization;
using System.Resources;

namespace Senparc.Xncf.Installer
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        private static readonly ResourceManager AreaResourceManager = new("Senparc.Xncf.Installer.InstallerResource", typeof(InstallerResource).Assembly);

        /// <summary>
        /// Resolves an installer area resource in the current UI culture and
        /// returns the supplied fallback when the key is unavailable.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback text.</param>
        /// <returns>The localized or fallback area text.</returns>
        private static string AreaT(string key, string fallback)
        {
            return AreaResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
        }

        #region Legacy section

        public string HomeUrl => "/Installer";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), AreaT("Installer.Area.Home", "首页"),"fa fa-laptop"),
			 		};

        /// <summary>
        /// Registers the installer Razor Pages and records area startup diagnostics.
        /// </summary>
        /// <param name="builder">The MVC builder being configured.</param>
        /// <param name="env">The host environment used by the area contract.</param>
        /// <returns>The configured MVC builder.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("Installer 启动", "完成 Area:Senparc.Xncf.Installer 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "src", "Extensions", "System", "Senparc.Xncf.Installer"));
        #endregion
    }
}
