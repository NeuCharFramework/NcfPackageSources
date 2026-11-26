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
    /// The current Entities exist only to help SenparcEntities generate migration information and have no special operational purpose.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
    public class BasePoolEntities_PostgreSQL : BasePoolEntities
    {
        public BasePoolEntities_PostgreSQL(DbContextOptions<BasePoolEntities_PostgreSQL> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2. Build the host project.</para>
    /// <para>3. Select the PostgreSQL provider configuration.</para>
    /// <para>4、run：PM> add-migration [updatename] -Context BasePoolEntities_PostgreSQL -o SystemEntities/Migrations/Migrations.PostgreSQL.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_PostgreSQL, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
        };

        public SenparcDbContextFactory_PostgreSQL()
            : base(
                 /* project root directory in Debug mode

                 /* used to find the App_Data folder and then locate the database connection-string configuration */
                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
        {

        }
    }
}
