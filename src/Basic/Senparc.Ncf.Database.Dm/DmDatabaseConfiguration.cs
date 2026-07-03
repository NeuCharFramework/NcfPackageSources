/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DmDatabaseConfiguration.cs
    文件功能描述：DmDatabaseConfiguration 相关实现
    
    
    创建标识：Senparc - 20240703
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Dm.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Database.Dm
{
    public class DmDatabaseConfiguration : DatabaseConfigurationBase<DmDbContextOptionsBuilder, DmOptionsExtension>
    {
        public DmDatabaseConfiguration()
        {
        }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Dm;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => 
            (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as DmDbContextOptionsBuilder;
            //typedBuilder.EnableRetryOnFailure(
            //           maxRetryCount: 5,
            //           maxRetryDelay: TimeSpan.FromSeconds(5),
            //           errorNumbersToAdd: new int[] { 2 });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => 
        (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
        {
            Console.WriteLine($"DM connectionString:{connectionString}");
            optionsBuilder.UseDm(connectionString, actionBase);
        };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            return $"BACKUP DATABASE FULL BACKUPSET '{backupFilePath}' compressed;";
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE '{tableName}';";
        }
    }
}
