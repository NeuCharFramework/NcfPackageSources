using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    /// <summary>
    /// Current Entities only exist to help SenparcEntities generate Migration information and have no special operational significance.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class BasePoolEntities_PostgreSQL : BasePoolEntities
    {
        public BasePoolEntities_PostgreSQL(DbContextOptions<BasePoolEntities_PostgreSQL> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }
    }

    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Set the current project as the startup item</para>
    /// <para>3. Open the [Package Explorer Console] and set the default project to the current project</para>
    /// <para>4. Run: PM> add-migration [update name] -Context BasePoolEntities_PostgreSQL -o SystemEntities/Migrations/Migrations.PostgreSQL.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL()
            : base(
                 /* Project root directory in Debug mode
                 /* Used to find the App_Data folder to find the database connection string configuration information */
                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
        {

        }
    }
}
