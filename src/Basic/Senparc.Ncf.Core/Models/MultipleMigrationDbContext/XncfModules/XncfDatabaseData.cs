using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// XNCF 数据库模块信息，仅在单独操作特定 XNCF 数据库模块时有用，其他情况下对象可能为 null
    /// </summary>
    public class XncfDatabaseData
    {
        public XncfDatabaseData(IXncfDatabase xncfDatabaseRegister, string assemblyName)
        {
            XncfDatabaseRegister = xncfDatabaseRegister;
            AssemblyName = assemblyName;
        }

        /// <summary>
        /// DbContext 类型
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister { get; set; }

        /// <summary>
        /// 指定程序集名称
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Migration History 表名
        /// </summary>
        public string DatabaseMigrationHistoryTableName => NcfDatabaseMigrationHelper.GetDatabaseMigrationHistoryTableName(XncfDatabaseRegister.DatabaseUniquePrefix);
    }
}
