
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
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class AIAgentsHubSenparcEntities_Dm : AIAgentsHubSenparcEntities
    {
        public AIAgentsHubSenparcEntities_Dm(DbContextOptions<AIAgentsHubSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// DbContext creation at design time (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1、Switch to Debug mode</para>
    /// <para>2、Run: PM> add-migration [Update name] -c AIAgentsHubSenparcEntities_Dm -o Domain/Migrations/Migrations.Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<AIAgentsHubSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
