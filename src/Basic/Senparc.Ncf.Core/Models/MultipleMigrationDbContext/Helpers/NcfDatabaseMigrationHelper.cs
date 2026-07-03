/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfDatabaseMigrationHelper.cs
    文件功能描述：NcfDatabaseMigrationHelper 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
