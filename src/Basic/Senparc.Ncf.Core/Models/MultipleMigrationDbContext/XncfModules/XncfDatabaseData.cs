/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDatabaseData.cs
    File description: XncfDatabaseData related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfDatabaseData
    {
        public XncfDatabaseData(IXncfDatabase xncfDatabaseRegister, string assemblyName)
        {
            XncfDatabaseRegister = xncfDatabaseRegister;
            AssemblyName = assemblyName;
        }

        /// <summary>
        /// DbContext type
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DatabaseMigrationHistoryTableName => NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(XncfDatabaseRegister.DatabaseUniquePrefix);
    }
}
