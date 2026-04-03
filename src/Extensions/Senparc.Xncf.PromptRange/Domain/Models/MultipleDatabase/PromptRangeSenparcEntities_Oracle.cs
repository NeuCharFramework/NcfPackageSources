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
    [MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
    public class PromptRangeSenparcEntities_Oracle : PromptRangeSenparcEntities
    {
        public PromptRangeSenparcEntities_Oracle(DbContextOptions<PromptRangeSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Run: PM> add-migration [update name] -c PromptRangeSenparcEntities_Oracle -o Domain/Migrations/Migrations.Oracle </para>
    /// </summary>
    public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<PromptRangeSenparcEntities_Oracle, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {
        }
    }
}