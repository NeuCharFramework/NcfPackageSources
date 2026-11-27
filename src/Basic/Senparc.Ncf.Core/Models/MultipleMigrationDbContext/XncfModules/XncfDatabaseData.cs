/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDatabaseData.cs
    File description: XncfDatabaseData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Carries XNCF database registration metadata into provider-specific configuration callbacks.
    /// </summary>
    public class XncfDatabaseData
    {
        /// <summary>
        /// Initializes provider configuration metadata for an XNCF database.
        /// </summary>
        /// <param name="xncfDatabaseRegister">The module database registration.</param>
        /// <param name="assemblyName">The assembly containing migrations.</param>
        public XncfDatabaseData(IXncfDatabase xncfDatabaseRegister, string assemblyName)
        {
            XncfDatabaseRegister = xncfDatabaseRegister;
            AssemblyName = assemblyName;
        }

        /// <summary>
        /// Gets or sets the XNCF database registration.
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister { get; set; }

        /// <summary>
        /// Gets or sets the assembly containing the module migrations.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Gets the migration-history table name derived from the registration prefix.
        /// </summary>
        public string DatabaseMigrationHistoryTableName => NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(XncfDatabaseRegister.DatabaseUniquePrefix);
    }
}
