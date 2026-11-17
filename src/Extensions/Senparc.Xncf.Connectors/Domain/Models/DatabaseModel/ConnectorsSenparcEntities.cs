/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorsSenparcEntities.cs
    File description: connector module database context

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 Updated module behavior and related support.

----------------------------------------------------------------*/

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
