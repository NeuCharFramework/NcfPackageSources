
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.AIKernel.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class AIKernelSenparcEntities_Oracle : AIKernelSenparcEntities
    {
        public AIKernelSenparcEntities_Oracle(DbContextOptions<AIKernelSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }
    

    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c AIKernelSenparcEntities_Oracle -o Domain/Migrations/Migrations/Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<AIKernelSenparcEntities_Oracle, Register>
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
