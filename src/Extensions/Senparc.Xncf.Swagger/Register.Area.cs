//using Enyim.Caching.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System.Collections.Generic;
using System.IO;

namespace Senparc.Xncf.Swagger
{
    public partial class Register : IAreaRegister //Register XNCF page interface (optional on demand)
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/Swagger/Index";


        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
                     };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(Utils.ConfigurationHelper.SWAGGER_ATUH_COOKIE, options =>
                {
                    if (Utils.ConfigurationHelper.CustsomSwaggerOptions.UseAdminAuth)
                    {
                        options.LoginPath = "/Admin/Login/";
                    }
                    else
                    { 
                        options.LoginPath = $"/{Utils.ConfigurationHelper.CustsomSwaggerOptions.RoutePrefix}/login.html";
                    }
                    options.Cookie.HttpOnly = false;
                });

            builder.AddRazorPagesOptions(options =>
            {
                //Page permissions can be configured here
            });

            SenparcTrace.SendCustomLog("Swagger 启动", "完成 Area:Senparc.Xncf.Swagger 注册");

            return builder;
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Swagger"));

        #endregion
    }
}
