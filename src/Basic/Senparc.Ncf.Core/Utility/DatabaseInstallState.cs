/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseInstallState.cs
    文件功能描述：DatabaseInstallState 相关实现

    创建标识：Senparc - 20260729

    修改标识：Senparc - 20260804
    修改描述：v0.28.0-preview5 新增数据库升级维护状态与可配置页脚安全处理

----------------------------------------------------------------*/

using System;
using System.Data.Common;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides the shared database-state checks used while entering the installer.
    /// </summary>
    public static class DatabaseInstallState
    {
        /// <summary>
        /// Determines whether an exception represents the expected pre-installation
        /// state of a database. Database providers and EF Core may wrap the actual
        /// provider exception, so the complete inner-exception chain is inspected.
        /// </summary>
        public static bool IsDatabaseUnavailableForInstallation(Exception exception)
        {
            // “column ... does not exist” 也包含 does not exist，必须先排除架构升级状态，
            // 防止 PostgreSQL 等提供程序把缺字段误判成首次安装。
            if (IsSchemaUpgradeRequired(exception))
            {
                return false;
            }

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is not DbException)
                {
                    continue;
                }

                var message = current.Message ?? string.Empty;
                if (message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("invalid object name", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Microsoft.Data.SqlClient can report an unavailable target as a
                // pre-login timeout while the database is not ready yet.
                if (message.Contains("connection timeout expired", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("pre-login", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // SQL Server reports a database that has not been created yet, or
                // cannot be opened by the configured login, with this message pair.
                if (message.Contains("cannot open database", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("requested by the login", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断数据库是否已经存在，但代码模型引用了尚未创建的字段。
        /// 该状态应进入升级维护页，而不是重新进入首次安装。
        /// </summary>
        public static bool IsSchemaUpgradeRequired(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is not DbException)
                {
                    continue;
                }

                var message = current.Message ?? string.Empty;
                if (message.Contains("invalid column name", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("unknown column", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("no such column", StringComparison.OrdinalIgnoreCase)
                    || (message.Contains("column", StringComparison.OrdinalIgnoreCase)
                        && message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                    || message.Contains("ora-00904", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("invalid identifier", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
