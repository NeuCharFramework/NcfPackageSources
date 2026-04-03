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
    /// Multiple database context (DbContext) configuration pool with IXncfDatabase Register type as Key
    /// </summary>
    public class XncfDatabaseDbContextPool :
        /*Concurrent*/Dictionary<Type/* IXncfDatabaseRegisterType */, Dictionary<MultipleDatabaseType, Type/* Database XncfDatabaseDbContext type */>>
    {
        #region 单例

        XncfDatabaseDbContextPool() { }

        /// <summary>
        ///Global singleton of DatabaseConfigurationFactory
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
        ///Add configuration
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType"></param>
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            //Check if IDatabaseRegister is already included 
            if (!this.ContainsKey(multiDbContextAttr.XncfDatabaseRegisterType))
            {
                //Add MultipleDatabaseType corresponding collection
                this[multiDbContextAttr.XncfDatabaseRegisterType] = new Dictionary<MultipleDatabaseType, Type>();
            }
            //Add configuration
            this[multiDbContextAttr.XncfDatabaseRegisterType][multiDbContextAttr.MultipleDatabaseType] = xncfDatabaseDbContextType;
        }
    }
}
