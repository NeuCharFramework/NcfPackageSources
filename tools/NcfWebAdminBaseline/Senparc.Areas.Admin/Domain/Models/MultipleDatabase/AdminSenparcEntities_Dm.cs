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
    /// The current Entities exist only to help SenparcEntities generate migration information and have no special operational purpose.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
    public class AdminSenparcEntities_Dm : AdminSenparcEntities
    {
        public AdminSenparcEntities_Dm(DbContextOptions<AdminSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// <para>4、run：PM> add-migration [updatename] -Context AdminSenparcEntities_Dm -o SystemEntities/Migrations/Migrations.Dm.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<AdminSenparcEntities_Dm, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Dm()
             : base(
                 /* project root directory in Debug mode

                 /* used to find the App_Data folder and then locate the database connection-string configuration */
                 SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
            Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//defaultconfiguration
        }
    }
}
