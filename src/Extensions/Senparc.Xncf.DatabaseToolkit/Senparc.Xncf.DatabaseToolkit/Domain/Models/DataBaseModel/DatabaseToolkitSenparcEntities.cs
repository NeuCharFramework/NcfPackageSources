/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DatabaseToolkitSenparcEntities.cs
    文件功能描述：DatabaseToolkitSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public class DatabaseToolkitSenparcEntities : XncfDatabaseDbContext
    {
        public DatabaseToolkitSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<DbConfig> DbConfigs { get; set; }
    }
}
