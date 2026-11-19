
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.Installer.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.Installer.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class InstallerSenparcEntities_PostgreSQL : InstallerSenparcEntities
    {
        public InstallerSenparcEntities_PostgreSQL(DbContextOptions<InstallerSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c InstallerSenparcEntities_PostgreSQL -o Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<InstallerSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };
       

        public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
