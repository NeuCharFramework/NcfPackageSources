
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.XncfModuleManager.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class XncfModuleManagerSenparcEntities_SqlServer : XncfModuleManagerSenparcEntities
    {
        public XncfModuleManagerSenparcEntities_SqlServer(DbContextOptions<XncfModuleManagerSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// Design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production).
    /// <para>1. Switch to Debug mode.</para>
    /// <para>2. Run: PM> add-migration [MigrationName] -c XncfModuleManagerSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<XncfModuleManagerSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Use another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
