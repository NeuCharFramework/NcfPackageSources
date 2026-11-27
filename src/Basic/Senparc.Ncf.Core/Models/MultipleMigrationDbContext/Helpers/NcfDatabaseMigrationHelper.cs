/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseMigrationHelper.cs
    File description: NcfDatabaseMigrationHelper related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Provides naming helpers for EF Core migration history tables.
    /// </summary>
    public static class NcfDatabaseMigrationHelper
    {
        /// <summary>
        /// Gets the reserved prefix used by system-level database objects.
        /// </summary>
        public const string SYSTEM_UNIQUE_PREFIX = "SYSTEM";

        /// <summary>
        /// Builds the migration-history table name for a logical database prefix.
        /// </summary>
        /// <param name="databaseUniquePrefix">The logical database prefix.</param>
        /// <returns>The provider-independent migration history table name.</returns>
        public static string GetDatabaseMigrationHistoryTableName(string databaseUniquePrefix)
        {
            if (!databaseUniquePrefix.IsNullOrWhiteSpace())
            {
                if (databaseUniquePrefix == SYSTEM_UNIQUE_PREFIX)
                {
                    return "__EFMigrationsHistory";
                }
                return "__" + databaseUniquePrefix + "_EFMigrationsHistory";
            }
            else
            {
                //Legacy comment translated to English.
                return "__" + "Unknown" + "_EFMigrationsHistory";
            }
        }

        /// <summary>
        /// Builds the migration-history table name from an XNCF database registration.
        /// </summary>
        /// <param name="xncfDatabaseRegister">The module database registration.</param>
        /// <returns>The provider-independent migration history table name.</returns>
        public static string GetDatabaseMigrationHistoryTableName(IXncfDatabase xncfDatabaseRegister)
        {
            return GetDatabaseMigrationHistoryTableName(xncfDatabaseRegister.DatabaseUniquePrefix);
        }
    }
}
