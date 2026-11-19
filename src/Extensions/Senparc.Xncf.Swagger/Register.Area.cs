/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.19.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
    public partial class Register : IAreaRegister //register the XNCF page interface (optional)
    {
        #region Legacy section

        public string HomeUrl => "/Admin/Swagger/Index";


        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaHomeUrl(), SwaggerResource.Get("Area.Home", "首页"),"fa fa-laptop"),
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
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("Swagger 启动", "完成 Area:Senparc.Xncf.Swagger 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Swagger"));

        #endregion
    }
}
