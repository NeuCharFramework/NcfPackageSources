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
    /// Multiple databases generate Migration entity classes, no need to consider
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false/*Not supported at the moment*/)]
    public class MultipleMigrationDbContextAttribute : Attribute
    {
        /// <summary>
        /// MultipleMigrationDbContext constructor. Specify a multi-database configuration.
        /// </summary>
        /// <param name="multipleDatabaseType">MultipleDatabaseType database type</param>
        /// <param name="xncfDatabaseRegisterType">XncfDatabase registration class type</param>
        // /// <param name="runtimeDbContextType">The unified database context type used when running</param>
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
