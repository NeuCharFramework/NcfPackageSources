/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenparcEntitiesBase.cs
    文件功能描述：SenparcEntitiesBase 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
