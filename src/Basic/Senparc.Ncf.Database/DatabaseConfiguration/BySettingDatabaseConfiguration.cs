/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BySettingDatabaseConfiguration.cs
    File description: BySettingDatabaseConfiguration related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// Placeholder database configuration used when no concrete provider is selected.
    /// Every operational member throws <see cref="NotImplementedException"/>.
    /// </summary>
    public class BySettingDatabaseConfiguration : IDatabaseConfiguration
    {
        /// <summary>
        /// Gets the database provider represented by this placeholder configuration.
        /// </summary>
        public MultipleDatabaseType MultipleDatabaseType => throw new NotImplementedException();

        /// <summary>
        /// Gets the base database-options callback.
        /// </summary>
        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionBase => throw new NotImplementedException();

        /// <summary>
        /// Gets the provider-specific database-options callback.
        /// </summary>
        public Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => throw new NotImplementedException();

        /// <summary>
        /// Gets the delegate that applies the provider-specific configuration to an EF Core options builder.
        /// </summary>
        public Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => throw new NotImplementedException();

        /// <summary>
        /// Gets the SQL statement used to back up a database.
        /// This legacy configuration is intentionally unsupported and always throws.
        /// </summary>
        /// <param name="dbConnection">The active database connection.</param>
        /// <param name="backupFilePath">The destination backup path.</param>
        /// <returns>Never returns because the provider is not implemented.</returns>
        /// <exception cref="NotImplementedException">Always thrown by this placeholder implementation.</exception>
        public string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the provider-specific SQL statement used to drop a table.
        /// This legacy configuration is intentionally unsupported and always throws.
        /// </summary>
        /// <param name="dbContext">The active database context.</param>
        /// <param name="tableName">The table name to drop.</param>
        /// <returns>Never returns because the provider is not implemented.</returns>
        /// <exception cref="NotImplementedException">Always thrown by this placeholder implementation.</exception>
        public string GetDropTableSql(DbContext dbContext, string tableName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Applies provider-specific options to an EF Core builder.
        /// This legacy configuration is intentionally unsupported and always throws.
        /// </summary>
        /// <param name="builder">The EF Core options builder.</param>
        /// <param name="connectionString">The provider connection string.</param>
        /// <param name="xncfDatabaseData">Optional XNCF database metadata.</param>
        /// <param name="dbContextOptionsAction">Optional provider-options callback.</param>
        /// <exception cref="NotImplementedException">Always thrown by this placeholder implementation.</exception>
        public void UseDatabase(DbContextOptionsBuilder builder, string connectionString, XncfDatabaseData xncfDatabaseData = null, Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null)
        {
            throw new NotImplementedException();
        }
    }
}
