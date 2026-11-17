/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfDatabaseMigrationHelper.cs

    File description: NcfDatabaseMigrationHelper related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class NcfDatabaseMigrationHelper
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string SYSTEM_UNIQUE_PREFIX = "SYSTEM";

        /// <summary>
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
                return "__" + "Unknown" + "_EFMigrationsHistory";
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static string GetDatabaseMigrationHistoryTableName(IXncfDatabase xncfDatabaseRegister)
        {
            return GetDatabaseMigrationHistoryTableName(xncfDatabaseRegister.DatabaseUniquePrefix);
        }
    }
}
