using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Xncf.PromptRange.Models.DatabaseModel;
using Microsoft.AspNetCore.Builder;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class PromptRangeSenparcEntities_SqlServer : PromptRangeSenparcEntities
    {
        public PromptRangeSenparcEntities_SqlServer(DbContextOptions<PromptRangeSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c PromptRangeSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<PromptRangeSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }
}