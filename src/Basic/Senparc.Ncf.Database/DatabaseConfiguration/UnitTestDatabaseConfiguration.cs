using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Database
{
    ///// <summary>
    ///// Use the configuration in appsettigs.json to obtain it, not as any actual database configuration.
    ///// </summary>
    //public class UnitTestDatabaseConfiguration : IDatabaseConfiguration
    //{
    //    /// <summary>
    //    /// The default total database in the unit test environment
    //    /// </summary>
    //    public static Type? UnitTestPillarDbContext { get; set; } = null;

    //    public MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.UnitTest;

    //    public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => throw new NotImplementedException();

    //    public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => throw new NotImplementedException();

    //    public Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => throw new NotImplementedException();

    //    public string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public string GetDropTableSql(DbContext dbContext, string tableName)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void UseDatabase(DbContextOptionsBuilder builder, string connectionString, XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
