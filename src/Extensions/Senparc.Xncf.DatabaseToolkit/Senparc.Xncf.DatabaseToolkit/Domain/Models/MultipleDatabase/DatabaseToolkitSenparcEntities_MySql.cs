using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Xncf.DatabaseToolkit.Models.MultipleDatabase
{
    /// <summary>
    /// MySQL-specific DatabaseToolkit context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class DatabaseToolkitSenparcEntities_MySql : DatabaseToolkitSenparcEntities, IMultipleMigrationDbContext
    {
        public DatabaseToolkitSenparcEntities_MySql(DbContextOptions<DatabaseToolkitSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C DatabaseToolkitSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<DatabaseToolkitSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
