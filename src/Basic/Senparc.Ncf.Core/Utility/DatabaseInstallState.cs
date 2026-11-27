/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseInstallState.cs
    File description: DatabaseInstallState related implementation

    Created by: Senparc - 20260729

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

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
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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
        /// Determines whether a database exception indicates that the database or schema
        /// is not available yet for installation.
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
