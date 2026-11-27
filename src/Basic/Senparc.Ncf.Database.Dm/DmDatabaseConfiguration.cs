/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DmDatabaseConfiguration.cs
    File description: DmDatabaseConfiguration related implementation

    Created by: Senparc - 20240703

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.7.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Dm.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Models;
using System;
using System.Data.Common;

namespace Senparc.Ncf.Database.Dm
{
    /// <summary>Configures the Dameng EF Core provider for NCF contexts.</summary>
    public class DmDatabaseConfiguration : DatabaseConfigurationBase<DmDbContextOptionsBuilder, DmOptionsExtension>
    {
        /// <summary>
        /// Initializes the database configuration for the Dameng provider.
        /// </summary>
        public DmDatabaseConfiguration()
        {
        }

        /// <inheritdoc />
        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Dm;

        /// <inheritdoc />
        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => 
            (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as DmDbContextOptionsBuilder;
            //typedBuilder.EnableRetryOnFailure(
            //           maxRetryCount: 5,
            //           maxRetryDelay: TimeSpan.FromSeconds(5),
            //           errorNumbersToAdd: new int[] { 2 });
        };

        /// <inheritdoc />
        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase => 
        (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
        {
            Console.WriteLine($"DM connectionString:{connectionString}");
            optionsBuilder.UseDm(connectionString, actionBase);
        };

        /// <inheritdoc />
        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            return $"BACKUP DATABASE FULL BACKUPSET '{backupFilePath}' compressed;";
        }

        /// <inheritdoc />
        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE '{tableName}';";
        }
    }
}
