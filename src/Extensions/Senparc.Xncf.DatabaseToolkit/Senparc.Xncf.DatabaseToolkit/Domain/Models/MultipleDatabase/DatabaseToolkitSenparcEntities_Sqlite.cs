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
    /// SQLite-specific DatabaseToolkit context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class DatabaseToolkitSenparcEntities_Sqlite : DatabaseToolkitSenparcEntities, IMultipleMigrationDbContext
    {
        public DatabaseToolkitSenparcEntities_Sqlite(DbContextOptions<DatabaseToolkitSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C DatabaseToolkitSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<DatabaseToolkitSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
