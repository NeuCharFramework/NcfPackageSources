/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OracleDatabaseConfiguration.cs
    File description: OracleDatabaseConfiguration related implementation

    Created by: Senparc - 20220814

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.10.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.10.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Oracle.EntityFrameworkCore.Infrastructure;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Database.Oracle
{
    /// <summary>
    /// Configures Oracle EF Core options used by NCF multi-database contexts.
    /// The selected Oracle SQL compatibility level is applied when the provider
    /// options callback is executed.
    /// </summary>
    public class OracleDatabaseConfiguration : DatabaseConfigurationBase<OracleDbContextOptionsBuilder, OracleOptionsExtension>
    {
        private static string _useOracleSQLCompatibility = null;

        /// <summary>
        /// Stores the legacy text representation of an Oracle SQL compatibility level.
        /// </summary>
        /// <param name="useOracleSQLCompatibility">
        /// The compatibility value accepted by the Oracle provider, such as
        /// <c>11</c> or <c>12</c>. The value is retained for compatibility with
        /// older callers; the current net10 implementation configures EF Core from
        /// the enum overload instead.
        /// </param>
        public static void SetUseOracleSQLCompatibility(string useOracleSQLCompatibility)
        {
            _useOracleSQLCompatibility = useOracleSQLCompatibility;
        }

        private static OracleSQLCompatibility _oracleSQLCompatibility = OracleSQLCompatibility.DatabaseVersion19;


        /// <summary>
        /// Selects the Oracle SQL compatibility level used by the EF Core provider.
        /// </summary>
        /// <param name="oracleSQLCompatibility">
        /// The Oracle provider compatibility level to pass to
        /// <see cref="OracleDbContextOptionsBuilder.UseOracleSQLCompatibility(OracleSQLCompatibility)"/>.
        /// </param>
        public static void SetUseOracleSQLCompatibility(OracleSQLCompatibility oracleSQLCompatibility)
        {
            _oracleSQLCompatibility = oracleSQLCompatibility;
        }

        /// <summary>
        /// Initializes the Oracle database configuration using the default compatibility level.
        /// </summary>
        public OracleDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as OracleDbContextOptionsBuilder;

            // Oracle 19c defaults to the provider's current compatibility mode.
            typedBuilder.UseOracleSQLCompatibility(_oracleSQLCompatibility);
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseOracle(connectionString, actionBase);//beta6
            };

        /// <inheritdoc />
        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            return $@"Backup Database {dbConnection.Database} To disk='{backupFilePath}'";
        }

        /// <inheritdoc />
        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE {tableName}";
        }


    }
}
