using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;
using Senparc.CO2NET.Extensions;
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
    public class SqliteDatabaseConfiguration : DatabaseConfigurationBase<SqliteDbContextOptionsBuilder, SqliteOptionsExtension>
    {
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Sqlite;

        //private static DbConnection CreateInMemoryDatabase(string connStr)
        //{
        //    var connection = new SqliteConnection(connStr);
        //    connection.Open();
        //    return connection;
        //}


        internal static string GetLocalConnectionString(string connectionString)
        {
            // Check and adjust paths  
            const string PREFIX = "Filename=";
            if (connectionString.StartsWith($"{PREFIX}\\") || connectionString.StartsWith($"{PREFIX}./") || connectionString.StartsWith($"{PREFIX}.\\"))
            {
                string relativePath = connectionString.Substring(PREFIX.Length); // Remove the "Filename=" prefix  
                string dbPath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                connectionString = $"{PREFIX}{dbPath}";
            }

            return connectionString;
        }

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                //Other more configurations

                // Check and adjust paths  
                connectionString = GetLocalConnectionString(connectionString);

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
