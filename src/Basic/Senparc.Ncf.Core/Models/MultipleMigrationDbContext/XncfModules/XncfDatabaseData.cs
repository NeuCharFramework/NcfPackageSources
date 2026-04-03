using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// XNCF database module information, only useful when operating a specific XNCF database module alone, the object may be null in other cases
    /// </summary>
    public class XncfDatabaseData
    {
        public XncfDatabaseData(IXncfDatabase xncfDatabaseRegister, string assemblyName)
        {
            XncfDatabaseRegister = xncfDatabaseRegister;
            AssemblyName = assemblyName;
        }

        /// <summary>
        ///DbContext type
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister { get; set; }

        /// <summary>
        ///Specify the assembly name
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        ///Migration History table name
        /// </summary>
        public string DatabaseMigrationHistoryTableName => NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(XncfDatabaseRegister.DatabaseUniquePrefix);
    }
}
