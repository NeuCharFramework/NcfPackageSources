using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.Domain.DatabaseModel
{
    /* illustrate:
     * The significance of making SenparcEntitiesMultiTenantBase independent as a layer is to separate objects that must support multi-tenancy (such as system tables).
     * This allows you to separately initialize a DbContext with only TenantInfo and no other system tables.
     * This can avoid an infinite loop situation:
     * 1. During the process of caching tenant information (TenantInfos) entered for the first time, all tables need to be initialized and OnModelCreating() executed.
     * OnModelCreating() is only executed once in the entire system service life cycle. At this time, the tenant information that relies on multi-tenant objects (such as system tables) must be determined immediately.
     * 2. However, the multi-tenant information has not been generated yet. If you try to obtain it, you will enter the loop of 1.
     */

    /// <summary>
    ///Multi-tenant EF Core DbContext
    /// </summary>
    public sealed class SenparcEntitiesMultiTenant : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        /// <summary>
        ///Multi-tenant information
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }


        public SenparcEntitiesMultiTenant(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

    }
}
