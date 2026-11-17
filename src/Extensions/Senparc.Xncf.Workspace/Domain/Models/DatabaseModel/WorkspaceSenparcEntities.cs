/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceSenparcEntities.cs
    File description: Workspace module database context

    Created by: Senparc - 20261021

    Modified by: Senparc - 20261021
    Change description: v0.1.1 provide the Workspace module database context

    Modified by: Senparc - 20261103
    Change description: v0.2.2 maintain the Workspace module database context

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Workspace.Domain.Models.DatabaseModel;

public class WorkspaceSenparcEntities : XncfDatabaseDbContext
{
    public WorkspaceSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<WorkspaceRecord> WorkspaceRecords { get; set; } = null!;
}
