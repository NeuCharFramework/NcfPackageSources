
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
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Senparc.Xncf.Tenant.Register))]
    public class TenantSenparcEntities_PostgreSQL : TenantSenparcEntities
    {
        public TenantSenparcEntities_PostgreSQL(DbContextOptions<TenantSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Run: PM> add-migration [update name] -c TenantSenparcEntities_PostgreSQL -o Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<TenantSenparcEntities_PostgreSQL, Senparc.Xncf.Tenant.Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
