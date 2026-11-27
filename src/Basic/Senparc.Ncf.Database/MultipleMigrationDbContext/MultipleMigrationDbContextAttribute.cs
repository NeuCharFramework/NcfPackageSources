/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MultipleMigrationDbContextAttribute.cs
    File description: MultipleMigrationDbContextAttribute related implementation

    Created by: Senparc - 20201007

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.21.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// Associates an XNCF database registration with a provider-specific migration context.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false/*temporarilynotsupport*/)]
    public class MultipleMigrationDbContextAttribute : Attribute
    {
        /// <summary>Initializes provider and XNCF registration metadata.</summary>
        /// <param name="multipleDatabaseType">The database provider type.</param>
        /// <param name="xncfDatabaseRegisterType">The XNCF registration type implementing <see cref="IXncfDatabase"/>.</param>
        /// <exception cref="NcfDatabaseException">Thrown when the registration type is null or does not implement <see cref="IXncfDatabase"/>.</exception>
        public MultipleMigrationDbContextAttribute(MultipleDatabaseType multipleDatabaseType,
            Type xncfDatabaseRegisterType/*, Type runtimeDbContextType*/)
        {
            if (xncfDatabaseRegisterType == null || !xncfDatabaseRegisterType.GetInterfaces().Contains(typeof(IXncfDatabase)))
            {
                throw new NcfDatabaseException($"xncfDatabaseRegisterType 不能为空，且对应类型必须实现 IXncfDatabase 接口！", null);
            }

            MultipleDatabaseType = multipleDatabaseType;
            XncfDatabaseRegisterType = xncfDatabaseRegisterType;
            //RuntimeDbContextType = runtimeDbContextType;
        }

        /// <summary>Gets or sets the database provider type.</summary>
        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        /// <summary>Gets or sets the XNCF database registration type.</summary>
        public Type XncfDatabaseRegisterType { get; set; }
        //public Type RuntimeDbContextType { get; set; }

        IXncfDatabase _xncfDatabaseRegister;
        /// <summary>
        /// Gets an instantiated XNCF database registration for this attribute.
        /// </summary>
        public IXncfDatabase XncfDatabaseRegister
        {
            get
            {
                if (_xncfDatabaseRegister == null)
                {
                    _xncfDatabaseRegister = Activator.CreateInstance(XncfDatabaseRegisterType) as IXncfDatabase;
                }
                return _xncfDatabaseRegister;
            }
        }
    }
}
