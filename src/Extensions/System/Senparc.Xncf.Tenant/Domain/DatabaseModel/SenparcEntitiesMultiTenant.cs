/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcEntitiesMultiTenant.cs
    File description: SenparcEntitiesMultiTenant related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
    /* description：

     * Legacy comment translated to English.

     * Legacy comment translated to English.

     * Legacy comment translated to English.

     *      Legacy comment translated to English.

     *          Legacy comment translated to English.

     *      Legacy comment translated to English.

     */

    /// <summary>
    /// multi-tenancy EF Core DbContext
    /// </summary>
    public sealed class SenparcEntitiesMultiTenant : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        /// <summary>
        /// multi-tenancyinformation
        /// </summary>
        public DbSet<TenantInfo> TenantInfos { get; set; }


        public SenparcEntitiesMultiTenant(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

    }
}
