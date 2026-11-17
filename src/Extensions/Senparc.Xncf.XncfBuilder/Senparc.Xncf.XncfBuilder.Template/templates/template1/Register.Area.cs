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

        public string HomeUrl => "/Admin/Template_XncfName/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),

                          new AreaPageMenuItem(GetAreaUrl($"/Admin/Template_XncfName/DatabaseSampleIndex"),"数据列表示例","fa fa-bookmark-o"),

                          new AreaPageMenuItem(GetAreaUrl($"/Admin/Template_XncfName/DatabaseSample"),"数据库操作示例","fa fa-bookmark-o")
			 		};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("Template_XncfName 启动", "完成 Area:Template_OrgName.Xncf.Template_XncfName 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Template_OrgName.Xncf.Template_XncfName"));
        #endregion
    }
}
