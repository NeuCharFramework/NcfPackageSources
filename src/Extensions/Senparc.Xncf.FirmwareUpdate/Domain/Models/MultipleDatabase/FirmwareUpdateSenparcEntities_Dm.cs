using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.Dm, typeof(Register))]
public class FirmwareUpdateSenparcEntities_Dm : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_Dm(DbContextOptions<FirmwareUpdateSenparcEntities_Dm> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_Dm : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_Dm, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.Dm", "Senparc.Ncf.Database.Dm", "DmDatabaseConfiguration");
    };

    public SenparcDbContextFactory_Dm() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
