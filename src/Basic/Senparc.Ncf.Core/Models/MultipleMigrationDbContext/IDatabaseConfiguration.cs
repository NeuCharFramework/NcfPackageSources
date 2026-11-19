/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDatabaseConfiguration.cs
    File description: IDatabaseConfiguration related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// databaseconfigurationinterface
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public interface IDatabaseConfiguration<TBuilder, TExtension> : IDatabaseConfiguration
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {

    }

    /// <summary>
    /// databaseconfigurationinterface
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// databasetype
        /// </summary>
        MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// <para>var builder = new DbContextOptionsBuilder&lt;TDbContext&gt;(); builder.UseSqlServer(sqlConnection, DbContextOptionsAction);</para>
        /// </summary>
        /// <param name="builder">DbContextOptionsBuilder</param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        void UseDatabase(DbContextOptionsBuilder builder, /*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null);

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="backupFilePath"></param>
        string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetDropTableSql(DbContext dbContext, string tableName);
    }
}