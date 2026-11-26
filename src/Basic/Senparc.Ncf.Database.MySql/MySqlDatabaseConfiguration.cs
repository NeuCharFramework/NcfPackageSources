/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MySqlDatabaseConfiguration.cs
    File description: MySqlDatabaseConfiguration related implementation

    Created by: Senparc - 20201004

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.20.5-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Senparc.Ncf.Database.MySql
{
    /// <summary>
    /// Configures Pomelo MySQL EF Core options, retry behavior, and table SQL
    /// for NCF database contexts.
    /// </summary>
    public class MySqlDatabaseConfiguration : DatabaseConfigurationBase<MySqlDbContextOptionsBuilder, MySqlOptionsExtension>
    {
        /// <summary>Initializes the MySQL database configuration.</summary>
        public MySqlDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.MySql;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as MySqlDbContextOptionsBuilder;
            typedBuilder.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(5),
                       errorNumbersToAdd: new int[] { 2 });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseMySql(connectionString,
                    ServerVersion.AutoDetect(connectionString), 
                    actionBase);//beta6
            };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            throw new NcfDatabaseException("Pomelo.EntityFrameworkCore.MySql 暂时不支持运行时备份，请使用命令行进行备份。Pomelo.EntityFrameworkCore.MySql v5.0 之后可支持。", DatabaseConfigurationFactory.Instance.Current.GetType(), null);
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            return $"DROP TABLE `{tableName}`";
        }
    }

    /// <summary>
    /// Configures a deterministic MySQL 8.0 server version for design-time
    /// migration and schema generation.
    /// </summary>
    /// <remarks>
    /// Runtime configuration uses server-version auto-detection; design-time
    /// operations use a fixed MySQL 8.0 version.
    /// </remarks>
    public sealed class MySqlDesignTimeDatabaseConfiguration : MySqlDatabaseConfiguration
    {
        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData,
            Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 0)),
                    actionBase);
    }
}
