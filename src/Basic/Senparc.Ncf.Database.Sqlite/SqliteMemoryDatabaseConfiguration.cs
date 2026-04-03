using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Database.Sqlite
{
    /// <summary>
    ///SQLite database configuration
    /// </summary>
    public class SqliteMemoryDatabaseConfiguration : DatabaseConfigurationBase<SqliteDbContextOptionsBuilder, SqliteOptionsExtension>
    {
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Sqlite;

        //private static DbConnection CreateInMemoryDatabase(string connStr)
        //{
        //    var connection = new SqliteConnection(connStr);
        //    connection.Open();
        //    return connection;
        //}

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                //Other more configurations

                connectionString = SqliteDatabaseConfiguration.GetLocalConnectionString(connectionString);

                //Execute UseSqlite (required)
                //optionsBuilder.UseSqlite(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseSqlite(connectionString, actionBase);
            };


        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //More independent configuration of database operations (not required)
        };


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            dbConnection.Open();
            (dbConnection as SqliteConnection).BackupDatabase(new SqliteConnection("data source='" + backupFilePath + "'"));
            dbConnection.Close();
            return null;
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE IF EXISTS {tableName}";
        }
    }
}
