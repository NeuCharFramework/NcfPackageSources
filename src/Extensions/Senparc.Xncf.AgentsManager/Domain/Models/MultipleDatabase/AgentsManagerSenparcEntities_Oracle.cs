
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.AgentsManager.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class AgentsManagerSenparcEntities_Oracle : AgentsManagerSenparcEntities
    {
        /// <summary>
        /// Initializes the Oracle-specific AgentsManager DbContext.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core options configured for Oracle.</param>
        public AgentsManagerSenparcEntities_Oracle(DbContextOptions<AgentsManagerSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AgentsManagerSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<AgentsManagerSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        /// <summary>
        /// Initializes the Oracle design-time DbContext factory using the configured root directory.
        /// </summary>
        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
