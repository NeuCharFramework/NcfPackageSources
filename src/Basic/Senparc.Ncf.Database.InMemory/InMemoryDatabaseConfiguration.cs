using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Database.InMemory
{
    /// <summary>
    /// SQLite 数据库配置
    /// </summary>
    public class InMemoryDatabaseConfiguration : DatabaseConfigurationBase<InMemoryDbContextOptionsBuilderForNcf, /*InMemoryOptionsExtension*/ InMemoryOptionsExtensionForNcf>
    {
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.InMemory;

        //private static DbConnection CreateInMemoryDatabase(string connStr)
        //{
        //    var connection = new InMemoryConnection(connStr);
        //    connection.Open();
        //    return connection;
        //}

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                //其他更多配置

                //执行 UseInMemory（必须）
                //optionsBuilder.UseInMemory(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseInMemoryDatabase(connectionString);
            };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //更多数据库操作独立配置（非必须）
        };


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //不需要用到
            return null;
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //不需要用到
            return null;
        }
    }
}
