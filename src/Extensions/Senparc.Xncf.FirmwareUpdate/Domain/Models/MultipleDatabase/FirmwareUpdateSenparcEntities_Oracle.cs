using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.Oracle, typeof(Register))]
public class FirmwareUpdateSenparcEntities_Oracle : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_Oracle(DbContextOptions<FirmwareUpdateSenparcEntities_Oracle> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_Oracle : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_Oracle, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.Oracle", "Senparc.Ncf.Database.Oracle", "OracleDatabaseConfiguration");
    };

    public SenparcDbContextFactory_Oracle() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
