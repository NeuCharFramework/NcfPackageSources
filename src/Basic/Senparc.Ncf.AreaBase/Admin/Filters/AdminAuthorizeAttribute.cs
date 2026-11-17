/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthorizeAttribute.cs
    File description: AdminAuthorizeAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// Current Area Authorization Handling Feature
    /// </summary>
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        //AuthorizeAttribute canand MVC common：https://docs.microsoft.com/en-us/aspnet/core/razor-pages/filter?view=aspnetcore-2.2
        public static string AuthenticationScheme => SiteConfig.NcfAdminAuthorizeScheme;

        public AdminAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
        public AdminAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
