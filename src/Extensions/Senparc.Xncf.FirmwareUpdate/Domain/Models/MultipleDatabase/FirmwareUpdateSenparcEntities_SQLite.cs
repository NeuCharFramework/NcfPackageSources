using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.Sqlite, typeof(Register))]
public class FirmwareUpdateSenparcEntities_Sqlite : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_Sqlite(DbContextOptions<FirmwareUpdateSenparcEntities_Sqlite> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_Sqlite : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_Sqlite, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.Sqlite", "Senparc.Ncf.Database.Sqlite", "SqliteMemoryDatabaseConfiguration");
    };

    public SenparcDbContextFactory_Sqlite() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
