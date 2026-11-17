/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InMemoryDatabaseConfiguration.cs
    File description: InMemoryDatabaseConfiguration related implementation

    Created by: Senparc - 20240721

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
    /// SQLite databaseconfiguration
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
                //Legacy comment translated to English.

                //execute UseInMemory（must）
                //optionsBuilder.UseInMemory(CreateInMemoryDatabase(connectionString), actionBase);

                optionsBuilder.UseInMemoryDatabase(connectionString);
            };

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (builder, xncfDatabaseData) =>
        {
            //Legacy comment translated to English.
        };


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //Legacy comment translated to English.
            return null;
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //Legacy comment translated to English.
            return null;
        }
    }
}
