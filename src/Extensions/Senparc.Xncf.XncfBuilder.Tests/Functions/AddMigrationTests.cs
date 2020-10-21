using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Xncf.XncfBuilder.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class AddMigrationTests : TestBase
    {
        [TestMethod]
        public void AddMigrationRunTest()
        {
            using (var service = base.ServiceCollection.BuildServiceProvider())
            {
                var function = new AddMigration(service);
                function.Run(new AddMigration.Parameters()
                {
                    DatabaseTypes = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList)
                    {
                        SelectedValues = new[] { MultipleDatabaseType.SQLite.ToString(), MultipleDatabaseType.SqlServer.ToString(), MultipleDatabaseType.MySql.ToString() }
                    },
                    DbContextName = "MyApp2SenparcEntities",
                    MigrationName = "AddTest",
                    ProjectPath = @"E:\Senparc项目\NeuCharFramework\NCF\src\SenparcDemo2.Xncf.MyApp2"
                });
            }
        }
    }
}
