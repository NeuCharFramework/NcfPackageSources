
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.AIAgentsHub.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class AIAgentsHubSenparcEntities_Sqlite : AIAgentsHubSenparcEntities
    {
        public AIAgentsHubSenparcEntities_Sqlite(DbContextOptions<AIAgentsHubSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AIAgentsHubSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<AIAgentsHubSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
