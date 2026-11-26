/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ISenparcEntitiesDbContext.cs
    File description: ISenparcEntitiesDbContext related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// Gets or sets the request tenant context used by the DbContext.
        /// </summary>
        MultiTenant.RequestTenantInfo TenantInfo { get; set; }

        /// Applies the global query filter for an entity type.
        /// <typeparam name="T">The entity type receiving the tenant/soft-delete filter.</typeparam>
        /// <param name="builder">The EF Core model builder.</param>
        void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase;

        /// <summary>
        /// Resets the migration state before a retry.
        /// </summary>
        void ResetMigrate();

        /// <summary>
        /// Applies pending database migrations for the current context.
        /// </summary>
        void Migrate();
    }
}
