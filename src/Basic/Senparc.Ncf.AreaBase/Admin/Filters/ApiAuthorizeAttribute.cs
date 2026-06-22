using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.AreaBase.Admin.Filters;
using System;

namespace Senparc.Xncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// API 权限验证特性
    /// 支持多种认证方案：Cookie (AdminAuthorize) 和 JWT (Bearer)
    /// </summary>
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 默认构造函数，支持 Cookie 和 JWT 两种认证方式
        /// </summary>
        public ApiAuthorizeAttribute()
        {
            // 支持多种认证方案：Admin Cookie 认证 和 JWT Bearer 认证
            // 只要任一方案通过，即可访问
            base.AuthenticationSchemes = $"{AdminAuthorizeAttribute.AuthenticationScheme},Bearer";
        }

        /// <summary>
        /// 带策略的构造函数
        /// </summary>
        /// <param name="policy">授权策略名称（如 "AdminOnly"）</param>
        public ApiAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
