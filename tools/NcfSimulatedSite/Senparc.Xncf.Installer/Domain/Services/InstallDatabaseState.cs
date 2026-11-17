/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: InstallDatabaseState.cs
    File description: InstallDatabaseState.cs related implementation


    Created by: Senparc - 20260728

    Modified by: Senparc - 20260729
    Change description: v0.4.1 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Xncf.Installer.Domain.Services
{
    internal static class InstallDatabaseState
    {
        /// <summary>
        /// Determines whether a database exception represents the expected
        /// pre-installation state. The installer can still render its configuration
        /// page in this state; the actual POST must still be able to create/update
        /// the database before installation can complete.
        /// </summary>
        public static bool IsDatabaseUnavailableForInstallation(Exception exception)
            => DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception);
    }
}
