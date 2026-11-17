using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin.Filters
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
