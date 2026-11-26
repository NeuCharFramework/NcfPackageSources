/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

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

namespace Senparc.Xncf.DynamicData
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

      //  public string HomeUrl => "/Admin/DynamicData/Index";

      //  public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
      //Legacy comment translated to English.
			 		//Legacy comment translated to English.
			 		//};

        /// <summary>
        /// Gets the DynamicData administration root URL.
        /// </summary>
        public string HomeUrl => "/Admin/DynamicData/";

        /// <summary>
        /// Gets the administration menu entries for page and table settings.
        /// </summary>
        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaUrl(HomeUrl+"PageSet"), DynamicDataResource.Get("Area.DynamicData.PageSettings", "界面设置"),"fa fa-laptop"),
                           new AreaPageMenuItem(GetAreaUrl(HomeUrl+"DataSheetSet"), DynamicDataResource.Get("Area.DynamicData.TableSettings", "数据库表设置"),"fa fa-laptop")
                     };

        /// <summary>
        /// Registers DynamicData Razor Page options and records module startup information.
        /// </summary>
        /// <param name="builder">The MVC builder to configure.</param>
        /// <param name="env">The host environment supplied by the application.</param>
        /// <returns>The configured MVC builder.</returns>
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("DynamicData 启动", "完成 Area:Senparc.Xncf.DynamicData 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        /// <summary>
        /// Gets the absolute path to the DynamicData assembly used for runtime compilation.
        /// </summary>
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.DynamicData"));
        #endregion
    }
}
