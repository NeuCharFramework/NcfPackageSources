/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDatabaseDbContextPool.cs
    File description: XncfDatabaseDbContextPool related implementation

    Created by: Senparc - 20201010

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

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
    /// Maps XNCF database registration types to provider-specific DbContext types.
    /// </summary>
    public class XncfDatabaseDbContextPool :
        /*Concurrent*/Dictionary<Type/* IXncfDatabase Register type */, Dictionary<MultipleDatabaseType, Type/* database XncfDatabaseDbContext type */>>
    {
        #region Legacy section

        XncfDatabaseDbContextPool() { }

        /// <summary>Gets the process-wide XNCF database-context pool.</summary>
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

        /// <summary>Registers a provider-specific DbContext type.</summary>
        /// <param name="multiDbContextAttr">The provider and registration metadata.</param>
        /// <param name="xncfDatabaseDbContextType">The concrete DbContext type.</param>
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            if (!this.ContainsKey(multiDbContextAttr.XncfDatabaseRegisterType))
            {
                this[multiDbContextAttr.XncfDatabaseRegisterType] = new Dictionary<MultipleDatabaseType, Type>();
            }
            this[multiDbContextAttr.XncfDatabaseRegisterType][multiDbContextAttr.MultipleDatabaseType] = xncfDatabaseDbContextType;
        }
    }
}
