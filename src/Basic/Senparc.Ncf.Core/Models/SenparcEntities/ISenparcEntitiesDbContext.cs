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
        /// Tenant information in the current context <![CDATA[If multi-tenancy is not enabled, the default value is NULL]]>
        /// </summary>
        MultiTenant.RequestTenantInfo TenantInfo { get; set; }

        void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase;

        /// <summary>
        ///Reset merge status
        /// </summary>
        void ResetMigrate();

        /// <summary>
        /// Perform the merge operation of EF Core (equivalent to update-database)
        /// <para>For security reasons, before each execution of the Migrate() method, ResetMigrate() must be executed to enable the state that allows Migrate execution. </para>
        /// </summary>
        void Migrate();
    }
}
