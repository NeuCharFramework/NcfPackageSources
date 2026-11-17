/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module

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

        public string HomeUrl => "/Admin/DynamicData/";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaUrl(HomeUrl+"PageSet"), DynamicDataResource.Get("Area.DynamicData.PageSettings", "界面设置"),"fa fa-laptop"),
                           new AreaPageMenuItem(GetAreaUrl(HomeUrl+"DataSheetSet"), DynamicDataResource.Get("Area.DynamicData.TableSettings", "数据库表设置"),"fa fa-laptop")
                     };

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
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.DynamicData"));
        #endregion
    }
}
