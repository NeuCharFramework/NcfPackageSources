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
    /// SQL Server-specific DatabaseToolkit context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class DatabaseToolkitSenparcEntities_SqlServer : DatabaseToolkitSenparcEntities, IMultipleMigrationDbContext
    {
        public DatabaseToolkitSenparcEntities_SqlServer(DbContextOptions<DatabaseToolkitSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C DatabaseToolkitSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<DatabaseToolkitSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
