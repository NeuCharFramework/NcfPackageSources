using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// XNCF 数据库模块信息，仅在单独操作特定 XNCF 数据库模块时有用，其他情况下对象可能为 null
    /// </summary>
    public class XncfDatabaseData
    {
        public XncfDatabaseData(Type xncfDatabaseDbContextType, string assemblyName, string databaseMigrationHistoryTableName, string databaseUniquePrefix)
        {
            XncfDatabaseDbContextType = xncfDatabaseDbContextType;
            AssemblyName = assemblyName;
            DatabaseMigrationHistoryTableName = databaseMigrationHistoryTableName;
            DatabaseUniquePrefix = databaseUniquePrefix;
        }

        /// <summary>
        /// DbContext 类型
        /// </summary>
        public Type XncfDatabaseDbContextType { get; set; }
        /// <summary>
        /// 指定程序集名称
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// Migration History 表名
        /// </summary>
        public string DatabaseMigrationHistoryTableName { get; set; }
        /// <summary>
        /// 数据库表全局唯一的前缀，务必避免和其他模块重复
        /// </summary>
        public string DatabaseUniquePrefix { get; set; }


    }
}
