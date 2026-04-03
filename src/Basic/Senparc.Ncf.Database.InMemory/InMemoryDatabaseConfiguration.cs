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
    ///SQLite database configuration
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
                //Other more configurations

                //Execute UseInMemory (required)
                //optionsBuilder.UseInMemory(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseInMemoryDatabase(connectionString);
            };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //More independent configuration of database operations (not required)
        };


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //No need to use
            return null;
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //No need to use
            return null;
        }
    }
}
