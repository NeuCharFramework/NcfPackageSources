using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.WeixinManager.Domain.Models.MultipleDatabase
{
    /// <summary>
    /// MySQL-specific WeixinManager database context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class WeixinSenparcEntities_MySql : WeixinSenparcEntities, IMultipleMigrationDbContext
    {
        /// <summary>
        /// Initializes the MySQL-specific WeixinManager DbContext.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core options configured for MySQL.</param>
        public WeixinSenparcEntities_MySql(DbContextOptions<WeixinSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C WeixinSenparcEntities_MySql -o Migrations/Migrations.MySql </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<WeixinSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDesignTimeDatabaseConfiguration");
        };

        /// <summary>
        /// Initializes the MySQL design-time DbContext factory.
        /// </summary>
        public SenparcDbContextFactory_MySql()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
