using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.PostgreSQL, typeof(Register))]
public class FirmwareUpdateSenparcEntities_PostgreSQL : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_PostgreSQL(DbContextOptions<FirmwareUpdateSenparcEntities_PostgreSQL> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_PostgreSQL : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_PostgreSQL, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.PostgreSQL", "Senparc.Ncf.Database.PostgreSQL", "PostgreSQLDatabaseConfiguration");
    };

    public SenparcDbContextFactory_PostgreSQL() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
