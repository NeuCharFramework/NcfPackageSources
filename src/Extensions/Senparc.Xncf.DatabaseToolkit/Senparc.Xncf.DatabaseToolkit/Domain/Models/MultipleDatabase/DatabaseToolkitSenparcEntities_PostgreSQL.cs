using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.Models.MultipleDatabase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class DatabaseToolkitSenparcEntities_PostgreSQL : DatabaseToolkitSenparcEntities, IMultipleMigrationDbContext
    {
        public DatabaseToolkitSenparcEntities_PostgreSQL(DbContextOptions<DatabaseToolkitSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C DatabaseToolkitSenparcEntities_PostgreSQL -o Domain/Migrations/Migrations.PostgreSQL </para>
    /// </summary>
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<DatabaseToolkitSenparcEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
