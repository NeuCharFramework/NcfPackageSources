using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// NCF 数据库帮助类
    /// </summary>
    public static class NcfDatabaseMigrationHelper
    {
        /// <summary>
        /// 系统表的数据库前缀，使用此前缀，将使用 __EFMigrationsHistory 表储存迁移记录，取代模块自定义表
        /// </summary>
        public const string SYSTEM_UNIQUE_PREFIX = "SYSTEM";

        /// <summary>
        /// 获取 EF Code First MigrationHistory 数据库表名
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
                //也可以抛出异常
                return "__" + "Unknown" + "_EFMigrationsHistory";
            }
        }

        /// <summary>
        /// 获取 EF Code First MigrationHistory 数据库表名
        /// </summary>
        /// <returns></returns>
        public static string GetDatabaseMigrationHistoryTableName(IXncfDatabase xncfDatabaseRegister)
        {
            return GetDatabaseMigrationHistoryTableName(xncfDatabaseRegister.DatabaseUniquePrefix);
        }
    }
}
