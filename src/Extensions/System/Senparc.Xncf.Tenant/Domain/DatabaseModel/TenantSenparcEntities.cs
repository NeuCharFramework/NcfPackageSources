/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: TenantSenparcEntities.cs

    File description: TenantSenparcEntities related implementation

    

    

    Created by: Senparc - 20211211

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public class TenantSenparcEntities : XncfDatabaseDbContext
    {
        public TenantSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region Legacy section
        public DbSet<TenantInfo> TenantInfos { get; set; }

        #endregion
    }
}
