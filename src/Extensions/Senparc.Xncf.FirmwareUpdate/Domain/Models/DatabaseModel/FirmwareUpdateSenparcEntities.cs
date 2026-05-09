using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.FirmwareUpdate.Models;

public class FirmwareUpdateSenparcEntities : XncfDatabaseDbContext
{
    public FirmwareUpdateSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Senparc.Xncf.FirmwareUpdate.FirmwareUpdateConfig> FirmwareUpdateConfigs { get; set; } = null!;
}
