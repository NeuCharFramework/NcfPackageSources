
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.IO;
using Senparc.Ncf.Core.Models;
using Microsoft.AspNetCore.Builder;

namespace Senparc.Xncf.Accounts.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
    public class AccountSenparcEntities_Sqlite : AccountSenparcEntities
    {
        public AccountSenparcEntities_Sqlite(DbContextOptions<AccountSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// Design-time DbContext creation (Code-First database migration is only used during development and will not be executed in the production environment)
    /// <para>1. Switch to Debug mode</para>
    /// <para>2. Run: PM> add-migration [update name] -c AccountSenparcEntities_Sqlite -o Domain/Migrations/Migrations.Sqlite </para>
    /// </summary>
    public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<AccountSenparcEntities_Sqlite, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //Specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
        };

        public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDictionaryPath)
        {

        }
    }
}
