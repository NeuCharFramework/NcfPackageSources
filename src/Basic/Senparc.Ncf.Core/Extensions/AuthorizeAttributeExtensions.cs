/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AuthorizeAttributeExtensions.cs
    File description: AuthorizeAttributeExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;

namespace Senparc.Ncf.Core.Extensions
{

    //Legacy comment translated to English.

    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public const string AuthenticationScheme = "NcfUserAuthorizeScheme";
        public UserAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
        public UserAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
    }
}
