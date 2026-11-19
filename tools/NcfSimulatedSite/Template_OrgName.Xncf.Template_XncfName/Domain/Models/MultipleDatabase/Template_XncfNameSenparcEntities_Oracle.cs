
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
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class Template_XncfNameSenparcEntities_Oracle : Template_XncfNameSenparcEntities
    {
        public Template_XncfNameSenparcEntities_Oracle(DbContextOptions<Template_XncfNameSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c Template_XncfNameSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<Template_XncfNameSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
