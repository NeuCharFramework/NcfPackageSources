using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.MySql, typeof(Register))]
public class FirmwareUpdateSenparcEntities_MySql : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_MySql(DbContextOptions<FirmwareUpdateSenparcEntities_MySql> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_MySql : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_MySql, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.MySql", "Senparc.Ncf.Database.MySql", "MySqlDatabaseConfiguration");
    };

    public SenparcDbContextFactory_MySql() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
