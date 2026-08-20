using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

[MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
public class FirmwareUpdateSenparcEntities_SqlServer : FirmwareUpdateSenparcEntities
{
    public FirmwareUpdateSenparcEntities_SqlServer(DbContextOptions<FirmwareUpdateSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
    {
    }
}

public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<FirmwareUpdateSenparcEntities_SqlServer, Register>
{
    protected override Action<IApplicationBuilder> AppAction => app =>
    {
        app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
    };

    public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
    {
    }
}
