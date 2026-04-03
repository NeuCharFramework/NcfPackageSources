using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// Authorization authentication attributes compatible with Cookie and JWT authentication
    /// Supports two authentication methods: NcfAdminAuthorizeScheme (Cookie) or Bearer_Backend (JWT)
    /// You can access it as long as one of the authentication passes
    /// </summary>
    public class AdminOrJwtAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        ///JWT authentication scheme name
        /// </summary>
        public const string JwtScheme = "Bearer_Backend";

        /// <summary>
        /// Constructor, configure to support two authentication schemes
        /// </summary>
        public AdminOrJwtAuthorizeAttribute()
        {
            // Separate multiple authentication schemes with commas, as long as one of them passes
            // NcfAdminAuthorizeScheme (Cookie authentication) + Bearer_Backend (JWT authentication)
            AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
        }

        /// <summary>
        /// Constructor, configure to support two authentication schemes and specify Policy
        /// </summary>
        /// <param name="policy">Authorization policy</param>
        public AdminOrJwtAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
