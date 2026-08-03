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
            // 此处仅验证 XncfBuilder 自身的六种数据库上下文，避免为了读取 DbSet
            // 启动完整 NCF 宿主及后台线程，导致单元测试受外部模块和运行环境影响。
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

            // 不序列化 System.Type，防止遍历庞大的反射对象图，并避免测试依赖 JSON 组件版本。
            var summary = allEntitySetInfo.Values.Select(info =>
                $"{info.SetName}: {info.DbSetType.FullName} -> " +
                string.Join(", ", info.SenparcEntityTypes.Select(type => type.FullName)));
            Console.WriteLine(string.Join(Environment.NewLine, summary));
        }
    }
}
