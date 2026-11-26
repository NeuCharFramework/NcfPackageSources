
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Mapping;

namespace Senparc.Xncf.AgentsManager.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class AgentsManagerSenparcEntities_Dm : AgentsManagerSenparcEntities
    {
        /// <summary>
        /// Initializes the DM-specific AgentsManager DbContext.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core options configured for the DM provider.</param>
        public AgentsManagerSenparcEntities_Dm(DbContextOptions<AgentsManagerSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AgentsManagerSenparcEntities_Dm -o Domain/Migrations/Migrations.Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<AgentsManagerSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        /// <summary>
        /// Initializes the DM design-time DbContext factory using the configured root directory.
        /// </summary>
        public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
