/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MultipleMigrationDbContextAttribute.cs
    File description: MultipleMigrationDbContextAttribute related implementation

    Created by: Senparc - 20201007

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

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
    ///Legacy comment translated to English.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false/*temporarilynotsupport*/)]
    public class MultipleMigrationDbContextAttribute : Attribute
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="multipleDatabaseType">MultipleDatabaseType databasetype</param>
        ///Legacy comment translated to English.
        //Legacy comment translated to English.
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

        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        public Type XncfDatabaseRegisterType { get; set; }
        //public Type RuntimeDbContextType { get; set; }

        IXncfDatabase _xncfDatabaseRegister;
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
