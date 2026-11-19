/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SenparcAdminAuthorizeAttribute.cs
    File description: SenparcAdminAuthorizeAttribute related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System;
using System.Security.Principal;
using Senparc.Mvc;
using Microsoft.AspNetCore.Http;
using Senparc.CO2NET;
using Senparc.Service;
using Senparc.CO2NET.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Areas.Admin.Filter
{
    public class SenparcAdminAuthorizeAttribute : SenparcAuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContext httpContext)
        {
            //return base.AuthorizeCore(httpContext);

            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            IPrincipal user = httpContext.User;
            if (!base.IsLogined(httpContext))
            {
                return false;//Legacy comment translated to English.
            }

            var result = base.AuthorizeCore(httpContext);// base.AuthorizeCore(httpContext);
            if (result)
            {
#if DEBUG
                var adminSession = httpContext.Session.GetString("AdminLogin") as string;
                if (adminSession.IsNullOrEmpty())
                {
                    try
                    {
                        var accountService = httpContext.RequestServices.GetService<AccountService>();
                        accountService.Logout();//Legacy comment translated to English.
                    }
                    catch
                    {
                    }
                    result = false;
                }
#endif
            }
            return result;
        }
    }
}
