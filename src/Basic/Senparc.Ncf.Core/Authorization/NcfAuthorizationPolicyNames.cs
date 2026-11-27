/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfAuthorizationPolicyNames.cs
    File description: Related implementation.

    Modified by: Senparc - 20260729
    Change description: v0.26.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// Contains the policy names used by NCF authorization configuration.
    /// </summary>
    public static class NcfAuthorizationPolicyNames
    {
        /// <summary>
        /// Requires an authenticated administrator.
        /// </summary>
        public const string AdminOnly = "AdminOnly";

        /// <summary>
        /// Requires the administrator member claim.
        /// </summary>
        public const string AdminMemberClaim = "AdminMember";
    }
}
