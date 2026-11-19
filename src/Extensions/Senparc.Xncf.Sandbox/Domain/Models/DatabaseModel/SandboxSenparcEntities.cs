/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SandboxSenparcEntities.cs
    File description: SandboxSenparcEntities related implementation

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Sandbox.Models;

public class SandboxSenparcEntities : XncfDatabaseDbContext
{
    public SandboxSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<SandboxSession> SandboxSessions { get; set; } = null!;

    //DOT REMOVE OR MODIFY THIS LINE - Entities Point
}
