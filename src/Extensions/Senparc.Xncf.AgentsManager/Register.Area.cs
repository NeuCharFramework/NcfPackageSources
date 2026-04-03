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
    public partial class Register : IAreaRegister, //Register XNCF page interface (optional on demand)
                                    IXncfRazorRuntimeCompilation  //Enable RazorPage runtime compilation
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/AgentsManager/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                    new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
			 		//new AreaPageMenuItem(GetAreaUrl($"/Admin/AgentsManager/DatabaseSample"),"Database Operation Example","fa fa-bookmark-o")
			};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //Page permissions can be configured here
            });

            SenparcTrace.SendCustomLog("AgentsManager 启动", "完成 Area:Senparc.Xncf.AgentsManager 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.AgentsManager"));
        #endregion
    }
}
