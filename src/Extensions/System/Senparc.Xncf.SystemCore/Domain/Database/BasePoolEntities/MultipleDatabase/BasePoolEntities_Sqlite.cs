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
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class BasePoolEntities_Sqlite : BasePoolEntities
    {
        public BasePoolEntities_Sqlite(DbContextOptions<BasePoolEntities_Sqlite> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// <para>4、run：PM> add-migration [updatename] -Context BasePoolEntities_SQLite -o SystemEntities/Migrations/Migrations.SQLite.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_SQLite : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SQLite()
            : base(
                 /* project root directory in Debug mode

                 /* used to find the App_Data folder and then locate the database connection-string configuration */
                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
        {
            //Senparc.Ncf.Core.Config.SiteConfig.SenparcCoreSetting.DatabaseName = "Local";//defaultconfiguration
        }
    }
}
