using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///NCF database help class
    /// </summary>
    public static class NcfDatabaseMigrationHelper
    {
        /// <summary>
        /// The database prefix of the system table. Using this prefix, the __EFMigrationsHistory table will be used to store migration records instead of the module custom table.
        /// </summary>
        public const string SYSTEM_UNIQUE_PREFIX = "SYSTEM";

        /// <summary>
        /// Get EF Code First MigrationHistory database table name
        /// </summary>
        /// <returns></returns>
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
                //Exceptions can also be thrown
                return "__" + "Unknown" + "_EFMigrationsHistory";
            }
        }

        /// <summary>
        /// Get EF Code First MigrationHistory database table name
        /// </summary>
        /// <returns></returns>
        public static string GetDatabaseMigrationHistoryTableName(IXncfDatabase xncfDatabaseRegister)
        {
            return GetDatabaseMigrationHistoryTableName(xncfDatabaseRegister.DatabaseUniquePrefix);
        }
    }
}
