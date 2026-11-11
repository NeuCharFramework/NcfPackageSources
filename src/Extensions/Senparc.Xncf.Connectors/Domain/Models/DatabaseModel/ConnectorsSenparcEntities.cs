using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;

public class ConnectorsSenparcEntities : XncfDatabaseDbContext
{
    public ConnectorsSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<ConnectorInstance> ConnectorInstances { get; set; } = null!;
}
