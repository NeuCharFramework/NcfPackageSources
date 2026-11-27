/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IDatabaseConfiguration.cs
    File description: IDatabaseConfiguration related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines the provider-specific EF Core configuration contract.
    /// </summary>
    public interface IDatabaseConfiguration<TBuilder, TExtension> : IDatabaseConfiguration
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {

    }

    /// <summary>
    /// Defines the database-provider configuration operations required by NCF.
    /// </summary>
    public interface IDatabaseConfiguration
    {
        /// <summary>
        /// Gets the configured database provider type.
        /// </summary>
        MultipleDatabaseType MultipleDatabaseType { get; }

        /// <summary>
        /// Gets the common EF Core options callback.
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase { get; }

        /// <summary>
        /// Gets the provider-specific EF Core options callback.
        /// </summary>
        Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension { get; }

        /// <summary>
        /// Gets the delegate that applies provider options to an EF Core builder.
        /// </summary>
        Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase { get; }

        /// <summary>
        /// Configures an EF Core options builder for the provider and connection string.
        /// </summary>
        /// <param name="builder">The EF Core options builder.</param>
        /// <param name="connectionString">The provider connection string.</param>
        /// <param name="xncfDatabaseData">Optional XNCF database metadata.</param>
        /// <param name="dbContextOptionsAction">Optional additional provider options callback.</param>
        void UseDatabase(DbContextOptionsBuilder builder, /*IRelationalDbContextOptionsBuilderInfrastructure optionsBuilder,*/ string connectionString,
                XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null);

        /// <summary>
        /// Builds the provider-specific database backup SQL statement.
        /// </summary>
        /// <param name="dbConnection">The active database connection.</param>
        /// <param name="backupFilePath">The backup destination path.</param>
        string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath);

        /// <summary>
        /// Builds the provider-specific table-drop SQL statement.
        /// </summary>
        /// <param name="dbContext">The active database context.</param>
        /// <param name="tableName">The table name to drop.</param>
        /// <returns>The provider-specific SQL statement.</returns>
        string GetDropTableSql(DbContext dbContext, string tableName);
    }
}
