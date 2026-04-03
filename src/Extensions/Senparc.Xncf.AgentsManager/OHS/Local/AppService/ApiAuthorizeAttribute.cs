using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.AreaBase.Admin.Filters;
using System;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// API permission verification feature
    /// Supports multiple authentication schemes: Cookie (AdminAuthorize) and JWT (Bearer)
    /// </summary>
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        ///Default constructor, supports two authentication methods: Cookie and JWT
        /// </summary>
        public ApiAuthorizeAttribute()
        {
            // Supports multiple authentication schemes: Admin Cookie authentication and JWT Bearer authentication
            // As long as any plan passes, you can access
            base.AuthenticationSchemes = $"{AdminAuthorizeAttribute.AuthenticationScheme},Bearer";
        }

        /// <summary>
        /// Constructor with strategy
        /// </summary>
        /// <param name="policy">Authorization policy name (such as "AdminOnly")</param>
        public ApiAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
