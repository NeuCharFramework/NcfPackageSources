
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Template_OrgName.Xncf.Template_XncfName.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
    public class Template_XncfNameSenparcEntities_MySql : Template_XncfNameSenparcEntities
    {
        public Template_XncfNameSenparcEntities_MySql(DbContextOptions<Template_XncfNameSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
        {
        }
    }

    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Run: PM> add-migration [update name] -c Template_XncfNameSenparcEntities_MySql -o Domain/Migrations/Migrations.MySql </para>
    /// </summary>
    public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities_MySql, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
        };

        public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
