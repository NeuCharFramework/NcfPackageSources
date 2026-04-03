using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// In a multi-database system, a specific database type
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
        //UnitTest = 100000,
        //TODO:More
    }
}
