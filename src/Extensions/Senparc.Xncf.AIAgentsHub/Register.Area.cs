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
    public partial class Register : IAreaRegister, //Register XNCF page interface (optional on demand)
                                    IXncfRazorRuntimeCompilation  //Enable RazorPage runtime compilation
    {
        #region IAreaRegister interface

        public string HomeUrl=> "/Admin/AIAgentsHub/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(),"front page","fa fa-laptop"),
			 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/AIAgentsHub/DatabaseSample"),"Database operation example","fa fa-bookmark-o")
			 		};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //Page permissions can be configured here
            });

            SenparcTrace.SendCustomLog("AIAgentsHub start up", "Complete Area:Senparc.Xncf.AIAgentsHub register");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation interface
        public string LibraryPath=> Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.AIAgentsHub"));
        #endregion
    }
}
