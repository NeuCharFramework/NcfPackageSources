/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SqliteDatabaseConfiguration.cs
    File description: SqliteDatabaseConfiguration related implementation

    Created by: Senparc - 20240316

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.20.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

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
    /// Configures SQLite EF Core options and resolves relative database filenames
    /// against the NCF root directory.
    /// </summary>
    public class SqliteDatabaseConfiguration : DatabaseConfigurationBase<SqliteDbContextOptionsBuilder, SqliteOptionsExtension>
    {
        /// <summary>Gets the SQLite provider type.</summary>
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Sqlite;

        //private static DbConnection CreateInMemoryDatabase(string connStr)
        //{
        //    var connection = new SqliteConnection(connStr);
        //    connection.Open();
        //    return connection;
        //}


        /// <summary>
        /// Resolves a relative SQLite filename against the NCF root directory.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string to normalize.</param>
        /// <returns>The connection string with a rooted filename when a relative filename is used.</returns>
        internal static string GetLocalConnectionString(string connectionString)
        {
            const string PREFIX = "Filename=";
            if (connectionString.StartsWith($"{PREFIX}\\") || connectionString.StartsWith($"{PREFIX}./") || connectionString.StartsWith($"{PREFIX}.\\"))
            {
                string relativePath = connectionString.Substring(PREFIX.Length);
                string dbPath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                connectionString = $"{PREFIX}{dbPath}";
            }

            return connectionString;
        }

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                connectionString = GetLocalConnectionString(connectionString);

                //execute UseSqlite（must）
                //optionsBuilder.UseSqlite(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseSqlite(connectionString, actionBase);
            };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
        };


        /// <inheritdoc />
        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            dbConnection.Open();
            (dbConnection as SqliteConnection).BackupDatabase(new SqliteConnection("data source='" + backupFilePath + "'"));
            dbConnection.Close();
            return null;
        }

        /// <inheritdoc />
        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE IF EXISTS {tableName}";
        }
    }
}
