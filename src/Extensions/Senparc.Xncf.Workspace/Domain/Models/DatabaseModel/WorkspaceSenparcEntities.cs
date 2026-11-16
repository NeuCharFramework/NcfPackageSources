/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkspaceSenparcEntities.cs
    File description: 工作区模块数据库上下文
    
    
    Created by: Senparc - 20261021
    
    Modified by: Senparc - 20261021
    Change description: v0.1.1 提供工作区模块数据库上下文

    Modified by: Senparc - 20261103
    Change description: v0.2.2 维护工作区模块数据库上下文

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
