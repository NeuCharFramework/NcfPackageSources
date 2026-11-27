/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoDbData.cs
    File description: TenantInfoDbData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.15.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.Models
{
    /// <summary>Provides tenant-specific database context access through the NCF data contract.</summary>
    public interface ITenantInfoDbData : INcfDbData
    {
        /// <summary>Gets the tenant database context.</summary>
        SenparcEntitiesMultiTenant DataContext { get; }
    }

    /// <summary>Wraps the tenant database context for repository and transaction services.</summary>
    public class TenantInfoDbData : NcfDbData, INcfDbData, ITenantInfoDbData
    {
        private SenparcEntitiesMultiTenant dataContext;

        public TenantInfoDbData(SenparcEntitiesMultiTenant senparcEntities)
        {
            dataContext = senparcEntities;
        }


        public SenparcEntitiesMultiTenant DataContext
        {
            get
            {
                return BaseDataContext as SenparcEntitiesMultiTenant;
            }
        }

        /// <inheritdoc />
        public override void CloseConnection()
        {
            //BaseDataContext.Database.Connection.Close();
            dataContext = null;
        }

        public override DbContext BaseDataContext
        {
            get
            {
                if (dataContext == null)
                {
                    // A closed wrapper intentionally remains disconnected; the scoped
                    // tenant context is recreated by dependency injection at the next boundary.
                }
                return dataContext;
            }

        }
    }
}
