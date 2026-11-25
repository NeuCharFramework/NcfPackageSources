/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminAuthorizeAttribute.cs
    File description: AdminAuthorizeAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

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

        /// <summary>
        /// Initializes authorization with the configured NCF administration authentication scheme.
        /// </summary>
        public AdminAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
        /// <summary>
        /// Initializes authorization with the configured scheme and a required policy.
        /// </summary>
        /// <param name="policy">The authorization policy name.</param>
        public AdminAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
