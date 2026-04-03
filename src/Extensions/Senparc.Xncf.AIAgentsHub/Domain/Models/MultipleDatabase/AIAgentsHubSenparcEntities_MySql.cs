
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.AIAgentsHub.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.AIAgentsHub.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class AIAgentsHubSenparcEntities_MySql : AIAgentsHubSenparcEntities
    {
        public AIAgentsHubSenparcEntities_MySql(DbContextOptions<AIAgentsHubSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// DbContext creation at design time (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1、Switch to Debug mode</para>
    /// <para>2、Run: PM> add-migration [Update name] -c AIAgentsHubSenparcEntities_MySql -o Domain/Migrations/Migrations.MySql </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<AIAgentsHubSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
