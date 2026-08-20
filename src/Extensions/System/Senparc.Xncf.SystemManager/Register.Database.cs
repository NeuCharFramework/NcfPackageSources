/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：Register.Database 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.SystemManager
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "SYSTEM_MANAGER_";//NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//系统表，

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {

        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
           
        }
    }
}
