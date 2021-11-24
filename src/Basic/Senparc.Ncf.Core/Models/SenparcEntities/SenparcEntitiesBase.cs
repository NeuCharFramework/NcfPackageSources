using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    public abstract class SenparcEntitiesBase : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        #region 多租户
        public DbSet<TenantInfo> TenantInfos { get; set; }

        #endregion

        protected SenparcEntitiesBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Console.WriteLine("\t SenparcEntitiesBase OnModelCreating");

            #region 不可修改系统表
            modelBuilder.ApplyConfiguration(new XncfModuleAccountConfigurationMapping());
            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
