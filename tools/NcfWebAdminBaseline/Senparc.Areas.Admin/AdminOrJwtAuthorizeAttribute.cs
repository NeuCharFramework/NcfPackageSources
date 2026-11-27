/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminOrJwtAuthorizeAttribute.cs
    File description: AdminOrJwtAuthorizeAttribute related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// Requires either the administrator cookie scheme or the backend JWT scheme.
    /// </summary>
    public class AdminOrJwtAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// The authentication scheme name used for backend JWTs.
        /// </summary>
        public const string JwtScheme = "Bearer_Backend";

        /// <summary>
        /// Initializes authorization with both administrator-cookie and backend-JWT schemes.
        /// </summary>
        public AdminOrJwtAuthorizeAttribute()
        {
            AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
        }

        /// <summary>
        /// Initializes authorization with both schemes and a policy name.
        /// </summary>
        /// <param name="policy">The authorization policy to require.</param>
        public AdminOrJwtAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
