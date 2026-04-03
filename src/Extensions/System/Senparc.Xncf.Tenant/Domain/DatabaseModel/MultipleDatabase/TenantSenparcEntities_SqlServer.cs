
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.Tanent.Domain.DatabaseModel
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Senparc.Xncf.Tenant.Register))]
    public class TenantSenparcEntities_SqlServer : TenantSenparcEntities
    {
        public TenantSenparcEntities_SqlServer(DbContextOptions<TenantSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Run: PM> add-migration [update name] -c TenantSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<TenantSenparcEntities_SqlServer, Senparc.Xncf.Tenant.Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
