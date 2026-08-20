/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BySettingDatabaseConfiguration.cs
    文件功能描述：BySettingDatabaseConfiguration 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 使用 appsettigs.json 中的配置进行获取，不作为任何实际使用的数据库配置
    /// </summary>
    public class BySettingDatabaseConfiguration : IDatabaseConfiguration
    {
        public MultipleDatabaseType MultipleDatabaseType => throw new NotImplementedException();

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => throw new NotImplementedException();

        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => throw new NotImplementedException();

        public Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => throw new NotImplementedException();

        public string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            throw new NotImplementedException();
        }

        public string GetDropTableSql(DbContext dbContext, string tableName)
        {
            throw new NotImplementedException();
        }

        public void UseDatabase(DbContextOptionsBuilder builder, string connectionString, XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            throw new NotImplementedException();
        }
    }
}
