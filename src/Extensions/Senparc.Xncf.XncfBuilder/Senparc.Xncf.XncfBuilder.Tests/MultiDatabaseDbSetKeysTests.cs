using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.XncfBuilder.Models.MultipleDatabase;
using System;
using System.Linq;

namespace Senparc.Xncf.XncfBuilder.Tests
{
    [TestClass]
    public class MultiDatabaseDbSetKeysTests
    {
        [TestMethod]
        public void RunTest()
        {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var dbContextTypes = new[]
            {
                typeof(XncfBuilderSenparcEntities_Sqlite),
                typeof(XncfBuilderSenparcEntities_SqlServer),
                typeof(XncfBuilderSenparcEntities_MySql),
                typeof(XncfBuilderSenparcEntities_PostgreSQL),
                typeof(XncfBuilderSenparcEntities_Oracle),
                typeof(XncfBuilderSenparcEntities_Dm)
            };

            foreach (var dbContextType in dbContextTypes)
            {
                EntitySetKeys.TryLoadSetInfo(dbContextType);
                var hasEntitySet = EntitySetKeys.GetAllEntitySetInfo().Values
                    .Any(info => info.SenparcEntityTypes.Contains(dbContextType));
                Assert.IsTrue(hasEntitySet, $"{dbContextType.Name} 未注册任何 DbSet。");
            }

            var allEntitySetInfo = EntitySetKeys.GetAllEntitySetInfo();
            Assert.IsTrue(allEntitySetInfo.Count > 0);

            //Legacy comment translated to English.
            var summary = allEntitySetInfo.Values.Select(info =>
                $"{info.SetName}: {info.DbSetType.FullName} -> " +
                string.Join(", ", info.SenparcEntityTypes.Select(type => type.FullName)));
            Console.WriteLine(string.Join(Environment.NewLine, summary));
        }
    }
}
