/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfDatabaseDbContextPool.cs
    文件功能描述：XncfDatabaseDbContextPool 相关实现
    
    
    创建标识：Senparc - 20201010
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 以 IXncfDatabase Register 类型为 Key 的多数据库上下文（DbContext）配置池
    /// </summary>
    public class XncfDatabaseDbContextPool :
        /*Concurrent*/Dictionary<Type/* IXncfDatabase Register 类型 */, Dictionary<MultipleDatabaseType, Type/* 数据库 XncfDatabaseDbContext 类型 */>>
    {
        #region 单例

        XncfDatabaseDbContextPool() { }

        /// <summary>
        /// DatabaseConfigurationFactory 的全局单例
        /// </summary>
        public static XncfDatabaseDbContextPool Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly XncfDatabaseDbContextPool instance = new XncfDatabaseDbContextPool();
        }

        #endregion

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType"></param>
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            //查看是否已经包含 IDatabaseRegister 
            if (!this.ContainsKey(multiDbContextAttr.XncfDatabaseRegisterType))
            {
                //添加 MultipleDatabaseType 对应集合
                this[multiDbContextAttr.XncfDatabaseRegisterType] = new Dictionary<MultipleDatabaseType, Type>();
            }
            //加入配置
            this[multiDbContextAttr.XncfDatabaseRegisterType][multiDbContextAttr.MultipleDatabaseType] = xncfDatabaseDbContextType;
        }
    }
}
