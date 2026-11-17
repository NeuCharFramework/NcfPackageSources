/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDatabaseDbContextPool.cs
    File description: XncfDatabaseDbContextPool related implementation

    Created by: Senparc - 20201010

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfDatabaseDbContextPool :
        /*Concurrent*/Dictionary<Type/* IXncfDatabase Register type */, Dictionary<MultipleDatabaseType, Type/* database XncfDatabaseDbContext type */>>
    {
        #region Legacy section

        XncfDatabaseDbContextPool() { }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType"></param>
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            //Legacy comment translated to English.
            if (!this.ContainsKey(multiDbContextAttr.XncfDatabaseRegisterType))
            {
                //Legacy comment translated to English.
                this[multiDbContextAttr.XncfDatabaseRegisterType] = new Dictionary<MultipleDatabaseType, Type>();
            }
            //Legacy comment translated to English.
            this[multiDbContextAttr.XncfDatabaseRegisterType][multiDbContextAttr.MultipleDatabaseType] = xncfDatabaseDbContextType;
        }
    }
}
