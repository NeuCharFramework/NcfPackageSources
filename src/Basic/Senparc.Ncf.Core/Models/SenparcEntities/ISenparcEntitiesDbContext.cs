/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ISenparcEntitiesDbContext.cs
    文件功能描述：ISenparcEntitiesDbContext 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// ISenparcEntities
    /// </summary>
    public interface ISenparcEntitiesDbContext : IDisposable /*, IInfrastructure<IServiceProvider>,*/ /*IDbContextDependencies,*/ /*IDbSetCache*/ /*IDbQueryCache, *//*,IDbContextPoolable*/
    {
        /// <summary>
        /// 当前上下文中租户信息 <![CDATA[未启用多租户则默认值为NULL值]]>
        /// </summary>
        MultiTenant.RequestTenantInfo TenantInfo { get; set; }

        void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase;

        /// <summary>
        /// 重置合并状态
        /// </summary>
        void ResetMigrate();

        /// <summary>
        /// 执行 EF Core 的合并操作（等价于 update-database）
        /// <para>出于安全考虑，每次执行 Migrate() 方法之前，必须先执行 ResetMigrate() 开启允许 Migrate 执行的状态。</para>
        /// </summary>
        void Migrate();
    }
}
