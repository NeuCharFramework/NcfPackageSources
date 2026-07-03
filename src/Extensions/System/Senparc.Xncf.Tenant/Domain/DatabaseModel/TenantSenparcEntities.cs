/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantSenparcEntities.cs
    文件功能描述：TenantSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20211211
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// 当前上下文不应该和租户无关
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
