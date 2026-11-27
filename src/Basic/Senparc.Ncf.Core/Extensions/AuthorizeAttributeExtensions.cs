/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthorizeAttributeExtensions.cs
    File description: AuthorizeAttributeExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;

namespace Senparc.Ncf.Core.Extensions
{

    //Legacy comment translated to English.

    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public const string AuthenticationScheme = "NcfUserAuthorizeScheme";
        /// <summary>
        /// Initializes the user authorization attribute with a policy.
        /// </summary>
        /// <param name="policy">The required policy name.</param>
        public UserAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
        /// <summary>
        /// Initializes the user authorization attribute with the NCF user scheme.
        /// </summary>
        public UserAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
    }
}
