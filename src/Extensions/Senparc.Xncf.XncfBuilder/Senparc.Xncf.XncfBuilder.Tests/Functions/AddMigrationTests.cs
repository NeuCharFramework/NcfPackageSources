using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Tests;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.XncfBuilder.OHS.Local;
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
        IServiceCollection _services;
        public AddMigrationTests()
        {
            
        }

        protected override void BeforeRegisterServiceCollection(IServiceCollection services)
        {
            base.BeforeRegisterServiceCollection(services);

            _services = services;
        }

        [TestMethod]
        public void AddMigrationRunTest()
        {
            using (var service = _services.BuildServiceProvider())
            {
                var function = new DatabaseMigrationsAppService(service);
                var result = function.AddMigration(new OHS.PL.DatabaseMigrations_MigrationRequest
                {
                    DatabaseTypes = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList)
                    {
                        SelectedValues = new[] { MultipleDatabaseType.Sqlite.ToString(),/*, MultipleDatabaseType.SqlServer.ToString(), */MultipleDatabaseType.MySql.ToString() }
                    },

                    //DbContextName = "XncfBuilderEntities",
                    //MigrationName = "AddConfig",
                    //ProjectPath = @"E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\Senparc.Xncf.XncfBuilder\Senparc.Xncf.XncfBuilder"

                    DbContextName = "MyAppSenparcEntities",
                    MigrationName = "Add_Counter",
                    ProjectPath = new SelectionList(SelectionType.DropDownList,
                    new List<SelectionItem>() {
                        new SelectionItem() {
                         Text=@"E:\Senparc项目\NeuCharFramework\NCF\src\SenparcLive.Xncf.MyApp",
                         Value=@"E:\Senparc项目\NeuCharFramework\NCF\src\SenparcLive.Xncf.MyApp",
                         DefaultSelected=true
                        }})
                });

                Console.WriteLine(result.ToJson(true).Replace("\\r", "\r").Replace("\\n", "\n"));
            }
        }
    }
}
