//using Microsoft.AspNetCore.Builder;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Senparc.Ncf.Core.Models;
//using Senparc.Ncf.Database;
//using Senparc.Ncf.XncfBase.Database;
//using System;
//using System.IO;

//namespace Senparc.Xncf.SystemCore.Domain.Database
//{
//    /// <summary>
//    /// The current Entities exist only to help SenparcEntities generate migration information and have no special operational purpose.
//    /// </summary>
//    [MultipleMigrationDbContext(MultipleDatabaseType.UnitTest, typeof(Register))]
//    public class BasePoolEntities_UnitTest : BasePoolEntities
//    {
//        public BasePoolEntities_UnitTest(DbContextOptions<BasePoolEntities_UnitTest> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
//        {
//        }
//    }

//    /// <summary>
//    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
//    /// <para>1、switch to Debug mode</para>
//Legacy comment translated to English.
//Legacy comment translated to English.
//    /// <para>4、run：PM> add-migration [updatename] -Context BasePoolEntities_SqlServer -o SystemEntities/Migrations/Migrations.SqlServer.SystemEntities</para>
//    /// </summary> 
//    public class SenparcDbContextFactory_UnitTest : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_UnitTest, Register>
//    {
//        protected override Action<IApplicationBuilder> AppAction => app =>
//        {
//            //specify another database
//            app.UseNcfDatabase("Senparc.Ncf.Database.UnitTest", "Senparc.Ncf.Database.UnitTest", "UnitTestDatabaseConfiguration");
//        };

//        public SenparcDbContextFactory_UnitTest()
//            : base(
//                 /* project root directory in Debug mode
//                 /* used to find the App_Data folder and then locate the database connection-string configuration */
//                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
//        {

//        }
//    }
//}
