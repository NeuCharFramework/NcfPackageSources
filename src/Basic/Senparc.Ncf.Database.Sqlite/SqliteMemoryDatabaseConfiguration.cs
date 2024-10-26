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
    /// SQLite 数据库配置
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
                //其他更多配置

                // 检查并调整路径  
                const string PREFIX = "Filename=";
                if (connectionString.StartsWith($"{PREFIX}\\") || connectionString.StartsWith("{PREFIX}./") || connectionString.StartsWith("Filename=.\\"))
                {
                    string relativePath = connectionString.Substring(PREFIX.Length); // 去掉 "Filename=" 前缀  
                    string dbPath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                    connectionString = $"{PREFIX}{dbPath}";
                }

                //执行 UseSqlite（必须）
                //optionsBuilder.UseSqlite(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseSqlite(connectionString, actionBase);
            };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //更多数据库操作独立配置（非必须）
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
