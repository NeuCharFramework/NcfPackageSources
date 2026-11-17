/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NcfAdminAuthorizationExtensions.cs
    File description: NCF Admin Authorization Policy Registration Extension

    Modified by: Senparc - 20260729
    Change description: v0.22.0-preview3 Unify AdminOnly authorization policy naming and provide host registration extension

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Ncf.AreaBase.Admin
{
    /// <summary>
    /// NCF Admin Authorization Policy Registration Extension.
    /// </summary>
    public static class NcfAdminAuthorizationExtensions
    {
        /// <summary>
        /// Register the AdminOnly policy for the NCF standard.
        /// The host can pre-register a policy with the same name to retain its own authorization requirements.
        /// </summary>
        public static AuthorizationOptions AddNcfAdminAuthorizationPolicies(this AuthorizationOptions options)
        {
            if (options.GetPolicy(NcfAuthorizationPolicyNames.AdminOnly) == null)
            {
                options.AddPolicy(NcfAuthorizationPolicyNames.AdminOnly, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(NcfAuthorizationPolicyNames.AdminMemberClaim);
                });
            }

            return options;
        }
    }
}
