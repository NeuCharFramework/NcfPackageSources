using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin
{
    /// <summary>
    ///Permission authentication
    /// </summary>
    public class BackendJwtAuthorizeAttribute: AuthorizeAttribute
    {
        public const string AuthenticationScheme = "Bearer_Backend";

        //public const string PolicyName = "backend";

        public BackendJwtAuthorizeAttribute()
        {
            AuthenticationSchemes = AuthenticationScheme;
            //base.Policy = PolicyName;
        }
    }
}
