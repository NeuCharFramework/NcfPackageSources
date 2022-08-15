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
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Database.Oracle
{
    /// <summary>
    /// Oracle 数据库配置
    /// <para>注意：如果使用 Oracle 12 以下的版本（11），需要在调用 <code>services.AddDatabase&lt;OracleDatabaseConfiguration&gt;();</code> 之前，使用 <code>SetUseOracleSQLCompatibility(string useOracleSQLCompatibility)</code> 方法设置版本号，如 11.2，则输入 "11"</para>
    /// </summary>
    public class OracleDatabaseConfiguration : DatabaseConfigurationBase<OracleDbContextOptionsBuilder, OracleOptionsExtension>
    {
        private static string _useOracleSQLCompatibility = null;

        /// <summary>
        /// 设置 UseOracleSQLCompatibility 的参数，如 11.2g，输入"11"，12g，输入"12"（默认为 >= 12，此时可不输入）
        /// <para>注意：此设置应该在执行 <code>.AddDatabase&lt;OracleDatabaseConfiguration&gt;();</code> 之前执行</para>
        /// </summary>
        /// <param name="useOracleSQLCompatibility"></param>
        public static void SetUseOracleSQLCompatibility(string useOracleSQLCompatibility)
        {
            _useOracleSQLCompatibility = useOracleSQLCompatibility;
        }

        public OracleDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as OracleDbContextOptionsBuilder;

            //This method accepts either a value of "11" or "12" (default). By default, generated SQL is compatible with database version 12 and higher. Customers using Oracle Database version 11.2 should set UseOracleSQLCompatibility("11").
            //https://docs.oracle.com/en/database/oracle/oracle-data-access-components/19.3/odpnt/EFCoreAPI.html#GUID-66247629-2670-44AA-AC55-849C367852AF
            if (!_useOracleSQLCompatibility.IsNullOrEmpty())
            {
                typedBuilder.UseOracleSQLCompatibility(_useOracleSQLCompatibility);
            }
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
