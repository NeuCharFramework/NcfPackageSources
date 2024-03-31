using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;

namespace Senparc.Ncf.Database.SqlServer
{
    /// <summary>
    /// SQL Server 数据库配置
    /// </summary>
    public class SqlServerDatabaseConfiguration : DatabaseConfigurationBase<SqlServerDbContextOptionsBuilder, SqlServerOptionsExtension>
    {
        public SqlServerDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.SqlServer;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as SqlServerDbContextOptionsBuilder;
            typedBuilder.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(5),
                       errorNumbersToAdd: new int[] { 2 });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseSqlServer(connectionString, actionBase);//beta6
            };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            return $@"Backup Database {dbConnection.Database} To disk='{backupFilePath}'";

            //TODO: with DIFFERENTIAL
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //var schma = dbContext.Model.FindEntityType(type).GetSchema();
            //TODO: 增加 schma
            return $"DROP TABLE {tableName}";
        }


    }
}
