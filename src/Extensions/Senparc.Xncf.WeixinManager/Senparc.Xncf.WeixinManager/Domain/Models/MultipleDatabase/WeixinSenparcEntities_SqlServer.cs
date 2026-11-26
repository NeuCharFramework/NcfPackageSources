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
    /// SQL Server-specific WeixinManager database context used by runtime registration and migrations.
    /// </summary>
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class WeixinSenparcEntities_SqlServer : WeixinSenparcEntities, IMultipleMigrationDbContext
    {
        /// <summary>
        /// Initializes the SQL Server-specific WeixinManager DbContext.
        /// </summary>
        /// <param name="dbContextOptions">The EF Core options configured for SQL Server.</param>
        public WeixinSenparcEntities_SqlServer(DbContextOptions<WeixinSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -C WeixinSenparcEntities_SqlServer -o Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<WeixinSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        /// <summary>
        /// Initializes the SQL Server design-time DbContext factory.
        /// </summary>
        public SenparcDbContextFactory_SqlServer()
            : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
