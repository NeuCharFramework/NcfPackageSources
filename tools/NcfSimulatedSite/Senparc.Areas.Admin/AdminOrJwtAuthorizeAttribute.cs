/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminOrJwtAuthorizeAttribute.cs
    File description: AdminOrJwtAuthorizeAttribute related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;

namespace Senparc.Areas.Admin
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminOrJwtAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string JwtScheme = "Bearer_Backend";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AdminOrJwtAuthorizeAttribute()
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        public AdminOrJwtAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
