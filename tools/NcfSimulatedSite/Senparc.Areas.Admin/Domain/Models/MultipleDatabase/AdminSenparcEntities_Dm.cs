using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;

namespace Senparc.Areas.Admin.Domain.Models
{
    /// <summary>
    /// Current Entities only exist to help SenparcEntities generate Migration information and have no special operational significance.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class AdminSenparcEntities_Dm : AdminSenparcEntities
    {
        public AdminSenparcEntities_Dm(DbContextOptions<AdminSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Set the current project as the startup item</para>
    /// <para>3. Open the [Package Explorer Console] and set the default project to the current project</para>
    /// <para>4. Run: PM> add-migration [update name] -Context AdminSenparcEntities_Dm -o SystemEntities/Migrations/Migrations.Dm.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<AdminSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm()
             : base(
                 /* Project root directory in Debug mode
                 /* Used to find the App_Data folder to find the database connection string configuration information */
                 SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//Default configuration
        }
    }
}
