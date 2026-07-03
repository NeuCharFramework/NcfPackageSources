/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfDatabaseData.cs
    文件功能描述：XncfDatabaseData 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
