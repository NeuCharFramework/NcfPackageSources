/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantRule.cs
    文件功能描述：TenantRule 相关实现
    
    
    创建标识：Senparc - 20210212
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// 租户规则
    /// </summary>
    public enum TenantRule
    {
        /// <summary>
        /// 以域名区分
        /// </summary>
        DomainName = 0,
        /// <summary>
        /// Request 请求中的 Header 信息
        /// </summary>
        RequestHeader = 1,
        /// <summary>
        /// 以登录输入的租户名称区分
        /// </summary>
        LoginInput = 2,
        /// <summary>
        /// 默认
        /// </summary>
        Default = LoginInput
    }
}
