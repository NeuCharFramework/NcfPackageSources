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
        ///Legacy comment translated to English.
        /// </summary>
        MultiTenant.RequestTenantInfo TenantInfo { get; set; }

        void SetGlobalQuery<T>(ModelBuilder builder) where T : EntityBase;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        void ResetMigrate();

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        void Migrate();
    }
}
