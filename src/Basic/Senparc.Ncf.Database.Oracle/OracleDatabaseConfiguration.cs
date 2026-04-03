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
    ///Oracle database configuration, Oracle version is not less than V12
    /// <para>Note: If you use a version below Oracle 12 (11), please use <code>OracleDatabaseConfigurationForV11</code> directly. Or use manual method control (not recommended): Before calling <code>services.AddDatabase&lt;OracleDatabaseConfiguration&gt;();</code>, use the <code>SetUseOracleSQLCompatibility(string useOracleSQLCompatibility)</code> method to set the version number, such as 11.2, enter "11"</para>
    /// </summary>
    public class OracleDatabaseConfiguration : DatabaseConfigurationBase<OracleDbContextOptionsBuilder, OracleOptionsExtension>
    {
        private static string _useOracleSQLCompatibility = null;

        /// <summary>
        /// Set the parameters of UseOracleSQLCompatibility, such as 11.2g, enter "11", 12g, enter "12" (the default is >= 12, you don’t need to enter it at this time)
        /// <para>Note: This setting should be performed before executing <code>.AddDatabase&lt;OracleDatabaseConfiguration&gt;();</code></para>
        /// </summary>
        /// <param name="useOracleSQLCompatibility"></param>
        public static void SetUseOracleSQLCompatibility(string useOracleSQLCompatibility)
        {
            _useOracleSQLCompatibility = useOracleSQLCompatibility;
        }

        private static OracleSQLCompatibility _oracleSQLCompatibility = OracleSQLCompatibility.DatabaseVersion19;


        /// <summary>
        ///Set the parameters of OracleSQLCompatibility, the default is 19
        /// <para>Note: This setting should be performed before executing <code>.AddDatabase&lt;OracleDatabaseConfiguration&gt;();</code></para>
        /// </summary>
        /// <param name="oracleSQLCompatibility"></param>
        public static void SetUseOracleSQLCompatibility(OracleSQLCompatibility oracleSQLCompatibility)
        {
            _oracleSQLCompatibility = oracleSQLCompatibility;
        }

        public OracleDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as OracleDbContextOptionsBuilder;

            //This method accepts either a value of "11" or "12" (default). By default, generated SQL is compatible with database version 12 and higher. Customers using Oracle Database version 11.2 should set UseOracleSQLCompatibility("11").
            //https://docs.oracle.com/en/database/oracle/oracle-data-access-components/19.3/odpnt/EFCoreAPI.html#GUID-66247629-2670-44AA-AC55-849C367852AF
//#if NETSTANDARD
//            if (!_useOracleSQLCompatibility.IsNullOrEmpty())
//            {
//                typedBuilder.UseOracleSQLCompatibility(_useOracleSQLCompatibility);
//            }
//#else
            typedBuilder.UseOracleSQLCompatibility(_oracleSQLCompatibility);
//#endif
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
            //TODO: Add schma
            return $"DROP TABLE {tableName}";
        }


    }
}
