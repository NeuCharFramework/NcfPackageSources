using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Xncf.XncfBuilder.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class AddMigrationTests : TestBase
    {
        public AddMigrationTests()
        {
            //base.ServiceCollection.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
        }

        [TestMethod]
        public void AddMigrationRunTest()
        {
            using (var service = base.ServiceCollection.BuildServiceProvider())
            {
                var function = new AddMigration(service);
                var result = function.Run(new AddMigration.Parameters()
                {
                    DatabaseTypes = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList)
                    {
                        SelectedValues = new[] { MultipleDatabaseType.SQLite.ToString(), MultipleDatabaseType.SqlServer.ToString()/*, MultipleDatabaseType.MySql.ToString()*/ }
                    },
                    DbContextName = "XncfBuilderEntities",
                    MigrationName = "AddTest",
                    ProjectPath = @"E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\Senparc.Xncf.XncfBuilder\Senparc.Xncf.XncfBuilder"
                });

                Console.WriteLine(result.ToJson(true).Replace("\\r", "\r").Replace("\\n", "\n"));
            }
        }
    }
}
