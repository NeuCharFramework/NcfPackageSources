/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantSenparcEntities.cs
    File description: TenantSenparcEntities related implementation

    Created by: Senparc - 20211211

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;

namespace Senparc.Xncf.Tenant.Domain.DatabaseModel
{
    /// <summary>
    /// EF Core context for persisted tenant records.
    /// </summary>
    public class TenantSenparcEntities : XncfDatabaseDbContext
    {
        /// <summary>
        /// Initializes the tenant database context.
        /// </summary>
        /// <param name="dbContextOptions">The provider-specific EF Core options.</param>
        public TenantSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the persisted tenant records.
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }
    }
}
