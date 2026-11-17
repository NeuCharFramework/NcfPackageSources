
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
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Senparc.Xncf.Tenant.Register))]
    public class TenantSenparcEntities_Oracle : TenantSenparcEntities
    {
        public TenantSenparcEntities_Oracle(DbContextOptions<TenantSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c TenantSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<TenantSenparcEntities_Oracle, Senparc.Xncf.Tenant.Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
