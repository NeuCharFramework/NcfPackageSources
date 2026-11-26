/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BackendJwtAuthorizeAttribute.cs
    File description: BackendJwtAuthorizeAttribute.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// Applies the backend bearer authentication scheme and an optional backend policy
    /// to an MVC controller or action.
    /// </summary>
    public class BackendJwtAuthorizeAttribute: AuthorizeAttribute
    {
        public const string AuthenticationScheme = "Bearer_Backend";
        public const string SuperAdminPolicyName = "AdminSuperAdmin";

        //public const string PolicyName = "backend";

        /// <summary>
        /// Initializes the attribute with the backend bearer scheme and optional policy.
        /// </summary>
        /// <param name="policy">The authorization policy name, or <see langword="null"/> for scheme-only authorization.</param>
        public BackendJwtAuthorizeAttribute(string policy = null)
        {
            AuthenticationSchemes = AuthenticationScheme;
            Policy = policy;
        }
    }
}
