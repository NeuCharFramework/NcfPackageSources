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
//    /// The current Entities only exist to help SenparcEntities generate Migration information and have no special operational significance.
//    /// </summary>
//    [MultipleMigrationDbContext(MultipleDatabaseType.UnitTest, typeof(Register))]
//    public class BasePoolEntities_UnitTest : BasePoolEntities
//    {
//        public BasePoolEntities_UnitTest(DbContextOptions<BasePoolEntities_UnitTest> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
//        {
//        }
//    }

//    /// <summary>
//    /// DbContext creation at design time (Code-First database migration is only used during development and will not be executed in the production environment)
//    /// <para>1. Switch to Debug mode</para>
//    /// <para>2. Set the current project as the startup item</para>
//    /// <para>3. Open the [Package Explorer Console] and set the default project to the current project</para>
//    /// <para>4. Run: PM> add-migration [update name] -Context BasePoolEntities_SqlServer -o SystemEntities/Migrations/Migrations.SqlServer.SystemEntities</para>
//    /// </summary> 
//    public class SenparcDbContextFactory_UnitTest : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_UnitTest, Register>
//    {
//        protected override Action<IApplicationBuilder> AppAction => app =>
//        {
//            //Specify other databases
//            app.UseNcfDatabase("Senparc.Ncf.Database.UnitTest", "Senparc.Ncf.Database.UnitTest", "UnitTestDatabaseConfiguration");
//        };

//        public SenparcDbContextFactory_UnitTest()
//            : base(
//                 /* Project root directory in Debug mode
//                 /* Used to find the App_Data folder to find the database connection string configuration information */
//                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
//        {

//        }
//    }
//}
