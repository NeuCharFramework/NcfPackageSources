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

namespace Senparc.Xncf.PromptRange
{
    public partial class Register : IAreaRegister, //Register XNCF page interface (optional on demand)
                                    IXncfRazorRuntimeCompilation  //Enable RazorPage runtime compilation
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/PromptRange/";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Index"),"首页","fa fa-laptop"),
                          //new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Model"),"model","fa fa-laptop"),
                           new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Prompt"),"PromptRange","fa fa-laptop"),
                          //new AreaPageMenuItem(GetAreaUrl($"/Admin/PromptRange/DatabaseSample"),"Database Operation Example","fa fa-bookmark-o")
                     };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //Page permissions can be configured here
            });

            SenparcTrace.SendCustomLog("PromptRange 启动", "完成 Area:Senparc.Xncf.PromptRange 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.PromptRange"));
        #endregion
    }
}
