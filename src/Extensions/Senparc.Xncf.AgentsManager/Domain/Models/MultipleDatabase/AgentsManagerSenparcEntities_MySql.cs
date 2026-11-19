
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
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class AgentsManagerSenparcEntities_MySql : AgentsManagerSenparcEntities
    {
        public AgentsManagerSenparcEntities_MySql(DbContextOptions<AgentsManagerSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AgentsManagerSenparcEntities_MySql -o Domain/Migrations/Migrations.MySql </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<AgentsManagerSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
