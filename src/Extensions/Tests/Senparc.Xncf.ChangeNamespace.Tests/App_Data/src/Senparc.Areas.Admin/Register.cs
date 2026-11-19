using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Areas.Admin.Filters;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin
{
    public class Register : IAreaRegister
    {
        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder)
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(AdminAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.AccessDeniedPath = "/Admin/Forbidden/";
                    options.LoginPath = "/Admin/Login/";
                    options.Cookie.HttpOnly = false;
                });
            builder.Services
                .AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                    {
                        policy.RequireClaim("AdminMember");
                    });
                });

            builder.AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizePage("/", "AdminOnly");//Legacy comment translated to English.
                options.Conventions.AllowAnonymousToPage("/Login");//Legacy comment translated to English.
                //Legacy comment translated to English.
            });

            SenparcTrace.SendCustomLog("系统启动", "完成 Area:Admin 注册");
           
            return builder;
        }
    }
}
