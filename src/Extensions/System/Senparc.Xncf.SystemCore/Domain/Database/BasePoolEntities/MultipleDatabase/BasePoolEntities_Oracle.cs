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
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class BasePoolEntities_Oracle : BasePoolEntities
    {
        public BasePoolEntities_Oracle(DbContextOptions<BasePoolEntities_Oracle> dbContextOptions, IServiceProvider serviceProvider) : base(dbContextOptions, serviceProvider)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// <para>4、run：PM> add-migration [updatename] -Context BasePoolEntities_Oracle -o SystemEntities/Migrations/Migrations.Oracle.SystemEntities</para>
    /// </summary> 
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<BasePoolEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle()
            : base(
                 /* project root directory in Debug mode

                 /* used to find the App_Data folder and then locate the database connection-string configuration */
                 Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}Senparc.Web"))
        {

        }
    }
}
