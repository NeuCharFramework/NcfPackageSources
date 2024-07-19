using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 多数据库系统中，特定的某个数据库类型
    /// </summary>
    public enum MultipleDatabaseType
    {
        Sqlite,
        SqlServer,
        MySql,
        InMemory,
        AzureCosmos,
        Oracle,
        PostgreSQL,
        Dm,
        Other = 99999,
        UnitTest = 100000,
        //TODO:更多
    }
}
