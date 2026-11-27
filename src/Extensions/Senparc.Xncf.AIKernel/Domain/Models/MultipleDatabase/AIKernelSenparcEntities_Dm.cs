
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.AIKernel.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class AIKernelSenparcEntities_Dm : AIKernelSenparcEntities
    {
        /// <summary>
        /// Initializes the DM-specific AIKernel DbContext.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core options configured for DM.</param>
        public AIKernelSenparcEntities_Dm(DbContextOptions<AIKernelSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AIKernelSenparcEntities_Dm -o Domain/Migrations/Dm </para>
    /// </summary>
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<AIKernelSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        /// <summary>
        /// Initializes the DM design-time DbContext factory.
        /// </summary>
        public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
