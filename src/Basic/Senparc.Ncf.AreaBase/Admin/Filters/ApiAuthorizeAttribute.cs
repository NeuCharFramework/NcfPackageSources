/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ApiAuthorizeAttribute.cs
    文件功能描述：ApiAuthorizeAttribute 相关实现
    
    
    创建标识：Senparc - 20260324
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

----------------------------------------------------------------*/

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
        /// 与 Admin 后台 JWT 注册方案一致（Register.cs 中 AddJwtBearer("Bearer_Backend")）
        /// </summary>
        public const string JwtBearerScheme = "Bearer_Backend";

        /// <summary>
        /// 默认构造函数，支持 Cookie 和 JWT 两种认证方式
        /// </summary>
        public ApiAuthorizeAttribute()
        {
            // 支持多种认证方案：Admin Cookie 认证 和 JWT Bearer 认证
            // 只要任一方案通过，即可访问
            base.AuthenticationSchemes = $"{AdminAuthorizeAttribute.AuthenticationScheme},{JwtBearerScheme}";
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
