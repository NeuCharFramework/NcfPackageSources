using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 多数据库生成 Migration 的实体类，无需考虑
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false/*暂时不支持*/)]
    public class MultipleMigrationDbContextAttribute : Attribute
    {
        public MultipleMigrationDbContextAttribute(MultipleDatabaseType multipleDatabaseType, Type xncfDatabaseRegisterType)
        {
            if (xncfDatabaseRegisterType == null || !xncfDatabaseRegisterType.GetInterfaces().Contains(typeof(IXncfDatabase)))
            {
                throw new NcfDatabaseException($"xncfDatabaseRegisterType 不能为空，且对应类型必须实现 IXncfDatabase 接口！", null);
            }

            MultipleDatabaseType = multipleDatabaseType;
            XncfDatabaseRegisterType = xncfDatabaseRegisterType;
        }

        public MultipleDatabaseType MultipleDatabaseType { get; set; }
        public Type XncfDatabaseRegisterType { get; set; }

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
