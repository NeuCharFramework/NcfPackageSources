using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Oracle.EntityFrameworkCore.Infrastructure;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;

namespace Senparc.Ncf.Database.Oracle
{
    /// <summary>
    /// SQL Server 数据库配置
    /// </summary>
    public class OracleDatabaseConfiguration : DatabaseConfigurationBase<OracleDbContextOptionsBuilder, OracleOptionsExtension>
    {
        public OracleDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as OracleDbContextOptionsBuilder;

            //This method accepts either a value of "11" or "12" (default). By default, generated SQL is compatible with database version 12 and higher. Customers using Oracle Database version 11.2 should set UseOracleSQLCompatibility("11").
            //https://docs.oracle.com/en/database/oracle/oracle-data-access-components/19.3/odpnt/EFCoreAPI.html#GUID-66247629-2670-44AA-AC55-849C367852AF
            //typedBuilder.UseOracleSQLCompatibility("11");
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseOracle(connectionString, actionBase);//beta6
            };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            return $@"Backup Database {dbConnection.Database} To disk='{backupFilePath}'";
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //var schma = dbContext.Model.FindEntityType(type).GetSchema();
            //TODO: 增加 schma
            return $"DROP TABLE {tableName}";
        }


    }
}
