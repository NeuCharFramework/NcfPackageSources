using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;

namespace Senparc.Xncf.Tenant.Domain.DatabaseModel
{
    /// <summary>
    /// The current context should not be irrelevant to the tenant
    /// </summary>
    public class TenantSenparcEntities : XncfDatabaseDbContext
    {
        public TenantSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        #region 多租户
        public DbSet<TenantInfo> TenantInfos { get; set; }

        #endregion
    }
}
