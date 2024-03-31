using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.Sqlite;
using Senparc.Ncf.Database.SqlServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Database.Tests
{
    [TestClass()]
    public class RegisterTests
    {
        [TestMethod()]
        public void UseNcfDatabaseTest()
        {
            var dbConfig = Activator.CreateInstance(typeof(SqliteDatabaseConfiguration), BindingFlags.IgnoreCase);
            Assert.IsNotNull(dbConfig);

            dbConfig = Activator.CreateInstance(typeof(SqlServerDatabaseConfiguration), BindingFlags.IgnoreCase);
            Assert.IsNotNull(dbConfig);
        }
    }
}