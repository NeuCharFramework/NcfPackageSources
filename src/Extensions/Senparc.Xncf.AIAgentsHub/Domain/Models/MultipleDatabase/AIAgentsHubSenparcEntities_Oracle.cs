
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
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class AIAgentsHubSenparcEntities_Oracle : AIAgentsHubSenparcEntities
    {
        public AIAgentsHubSenparcEntities_Oracle(DbContextOptions<AIAgentsHubSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// DbContext creation at design time (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1、Switch to Debug mode</para>
    /// <para>2、Run: PM> add-migration [Update name] -c AIAgentsHubSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<AIAgentsHubSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
