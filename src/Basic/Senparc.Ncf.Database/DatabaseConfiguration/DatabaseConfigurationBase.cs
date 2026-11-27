/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseConfigurationBase.cs
    File description: DatabaseConfigurationBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.21.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// Provides the shared EF Core configuration pipeline used by concrete NCF database providers.
    /// </summary>
    public abstract class DatabaseConfigurationBase<TBuilder, TExtension> : IDatabaseConfiguration<TBuilder, TExtension>
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension :  RelationalOptionsExtension, new()
    {
        /// <summary>
        /// Gets the database provider represented by the concrete configuration.
        /// </summary>
        public abstract MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// Gets the typed callback that forwards to the common database-options callback.
        /// </summary>
        public virtual Action<TBuilder, XncfDatabaseData> TypedDbContextOptionsActionBase => DbContextOptionsActionBase;

        /// <summary>
        /// Gets the common callback that configures migration assembly and history-table metadata
        /// for an XNCF database context.
        /// </summary>
        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => (builder, xncfDatabaseData) =>
         {

             var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
             if (xncfDatabaseData != null)
             {
                 var typedBuilder = builder as TBuilder;

                 var dbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(xncfDatabaseData.XncfDatabaseRegister.GetType());

                 var dbContextAssemblyName = xncfDatabaseData.AssemblyName ?? dbContextType.Assembly.FullName;
                 var databaseMigrationHistoryTableName = NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(xncfDatabaseData.XncfDatabaseRegister);

                 typedBuilder.MigrationsAssembly(dbContextAssemblyName)
                        .MigrationsHistoryTable(databaseMigrationHistoryTableName);
             }
             else
             {
             }
         };

        /// <summary>
        /// Gets the provider-specific callback supplied by the concrete database configuration.
        /// </summary>
        public abstract Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        /// <summary>
        /// Gets the delegate that selects and configures the provider for an EF Core options builder.
        /// </summary>
        public abstract Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// Builds the provider-specific SQL statement for backing up a database.
        /// </summary>
        /// <param name="dbConnection">The active database connection.</param>
        /// <param name="backupFilePath">The destination backup path.</param>
        /// <returns>The provider-specific backup statement.</returns>
        public abstract string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);
        /// <summary>
        /// Builds the provider-specific SQL statement for dropping a table.
        /// </summary>
        /// <param name="dbContext">The active database context.</param>
        /// <param name="tableName">The table name to drop.</param>
        /// <returns>The provider-specific drop-table statement.</returns>
        public abstract string GetDropTableSql(DbContext dbContext, string tableName);

        /// <summary>
        /// Applies the common, provider-specific, and caller-supplied callbacks to an EF Core
        /// options builder using the concrete database provider.
        /// </summary>
        /// <param name="builder">The EF Core options builder to configure.</param>
        /// <param name="connectionString">The database connection string.</param>
        /// <param name="xncfDatabaseData">Optional XNCF database registration metadata.</param>
        /// <param name="dbContextOptionsAction">An optional callback for additional provider options.</param>
        public void UseDatabase(DbContextOptionsBuilder builder,/*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, 
                Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            SetUseDatabase(builder, connectionString, xncfDatabaseData, b =>
                {
                    //executebasecode
                    DbContextOptionsActionBase(b, xncfDatabaseData);
                    //executeextensioncode
                    DbContextOptionsActionExtension?.Invoke(b, xncfDatabaseData);
                    dbContextOptionsAction?.Invoke(b, xncfDatabaseData);
                }
            );
        }

    }
}
