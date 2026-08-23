using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Ncf.Database.Tests
{
    [TestClass]
    public class MultipleDatabasePoolMatrixLogTests
    {
        private sealed class FakeRegisterA { }
        private sealed class FakeRegisterB { }
        private sealed class FakeDbContext_SqlServer { }
        private sealed class FakeDbContext_Sqlite { }

        [TestMethod]
        public void BuildSupportMatrixLog_MarksCurrentAndListsDbContext()
        {
            var map = new Dictionary<Type, Dictionary<MultipleDatabaseType, Type>>
            {
                [typeof(FakeRegisterA)] = new Dictionary<MultipleDatabaseType, Type>
                {
                    [MultipleDatabaseType.Sqlite] = typeof(FakeDbContext_Sqlite),
                    [MultipleDatabaseType.SqlServer] = typeof(FakeDbContext_SqlServer),
                },
                [typeof(FakeRegisterB)] = new Dictionary<MultipleDatabaseType, Type>
                {
                    // 仅支持 Sqlite，用于验证当前 SqlServer 绑定表中出现 (not supported)
                    [MultipleDatabaseType.Sqlite] = typeof(FakeDbContext_Sqlite),
                },
            };

            var log = MultipleDatabasePool.BuildSupportMatrixLog(map, MultipleDatabaseType.SqlServer);

            StringAssert.Contains(log, "Current Database: SqlServer");
            StringAssert.Contains(log, "[*]=IN USE");
            StringAssert.Contains(log, "FakeDbContext_SqlServer");
            StringAssert.Contains(log, typeof(FakeRegisterA).FullName);
            StringAssert.Contains(log, typeof(FakeRegisterB).FullName);
            Assert.IsTrue(log.Contains("| *") || log.Contains("| * "), "当前库列应为 *");
            StringAssert.Contains(log, "(not supported)");
        }

        [TestMethod]
        public void BuildSupportMatrixLog_WithoutCurrent_SkipsBindingTable()
        {
            var map = new Dictionary<Type, Dictionary<MultipleDatabaseType, Type>>
            {
                [typeof(FakeRegisterA)] = new Dictionary<MultipleDatabaseType, Type>
                {
                    [MultipleDatabaseType.Sqlite] = typeof(FakeDbContext_Sqlite),
                },
            };

            var log = MultipleDatabasePool.BuildSupportMatrixLog(map, currentDatabaseType: null);

            StringAssert.Contains(log, "Current Database: (unknown at scan time)");
            StringAssert.Contains(log, "skipped: current database type not available");
        }
    }
}
