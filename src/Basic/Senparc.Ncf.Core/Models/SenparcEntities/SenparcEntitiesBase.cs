using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    public abstract class SenparcEntitiesBase : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        protected SenparcEntitiesBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Console.WriteLine("\t SenparcEntitiesBase OnModelCreating");

            #region 不可修改系统表
            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
