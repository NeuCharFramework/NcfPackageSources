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

        private static string AreaT(string key, string fallback)
        {
            return AreaResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
        }

        #region Legacy section

        public string HomeUrl => "/Installer";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(), AreaT("Installer.Area.Home", "首页"),"fa fa-laptop"),
			 		};

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
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Installer"));
        #endregion
    }
}
