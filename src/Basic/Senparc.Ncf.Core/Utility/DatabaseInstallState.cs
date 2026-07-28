/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DatabaseInstallState.cs
    文件功能描述：DatabaseInstallState 相关实现

    创建标识：Senparc - 20260729

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
    }
}
