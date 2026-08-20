/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfAdminAuthorizationExtensions.cs
    文件功能描述：NCF Admin 授权策略注册扩展

    修改标识：Senparc - 20260729
    修改描述：v0.22.0-preview3 统一 AdminOnly 授权策略命名并提供宿主注册扩展

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Authorization;

namespace Senparc.Ncf.AreaBase.Admin
{
    /// <summary>
    /// NCF Admin 授权策略注册扩展。
    /// </summary>
    public static class NcfAdminAuthorizationExtensions
    {
        /// <summary>
        /// 注册 NCF 标准的 AdminOnly 策略。
        /// 宿主可以预先注册同名策略，以保留宿主自己的授权要求。
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
