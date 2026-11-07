/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfTenantNotFoundException.cs
    文件功能描述：请求携带的 TenantKey 无法匹配到任何租户时抛出的异常（多租户启用状态下的 fail-closed 拒绝）
    
    
    创建标识：Senparc - 20261104
    
    修改标识：Senparc - 20261104
    修改描述：vNext 新增 NcfTenantNotFoundException（安全修复：防止无效 TenantKey 静默降级到默认租户）

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.30.5-preview11 新增租户未找到异常（TenantKey fail-closed）
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// 请求中携带的 TenantKey 无法匹配到任何租户时抛出（仅当多租户启用时）。
    /// 用于 fail-closed：拒绝请求而不是静默降级到默认租户（Id=0）。
    /// </summary>
    public class NcfTenantNotFoundException : NcfTenantException
    {
        /// <summary>
        /// 无法匹配的 TenantKey
        /// </summary>
        public string TenantKey { get; }

        public NcfTenantNotFoundException(string tenantKey, bool logged = false)
            : base($"TenantKey '{tenantKey}' 无法匹配到任何租户，请求已被拒绝", logged)
        {
            this.TenantKey = tenantKey;
        }

        public NcfTenantNotFoundException(string tenantKey, Exception inner, bool logged = false)
            : base($"TenantKey '{tenantKey}' 无法匹配到任何租户，请求已被拒绝", inner, logged)
        {
            this.TenantKey = tenantKey;
        }
    }
}
