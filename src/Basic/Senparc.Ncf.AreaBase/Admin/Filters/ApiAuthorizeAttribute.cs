/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAuthorizeAttribute.cs
    File description: ApiAuthorizeAttribute related implementation

    Created by: Senparc - 20260324

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.AreaBase.Admin.Filters;
using System;

namespace Senparc.Xncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// API Permission Validation Feature
    /// Supports multiple authentication schemes: Cookie (AdminAuthorize) and JWT (Bearer)
    /// </summary>
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// and Admin backend JWT registration scheme are consistent (AddJwtBearer("Bearer_Backend") in Register.cs)
        /// </summary>
        public const string JwtBearerScheme = "Bearer_Backend";

        /// <summary>
        /// defaultconstructor, supports two authentication methods: Cookie and JWT
        /// </summary>
        public ApiAuthorizeAttribute()
        {
            // Supports multiple authentication methods: Admin Cookie Authentication and JWT Bearer Authentication
            // Accessible as long as any plan is approved
            base.AuthenticationSchemes = $"{AdminAuthorizeAttribute.AuthenticationScheme},{JwtBearerScheme}";
        }

        /// <summary>
        /// Constructor with strategy
        /// </summary>
        /// <param name="policy">Authorization policy name (e.g., "AdminOnly")</param>
        public ApiAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
