/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkspaceSenparcEntities.cs
    文件功能描述：工作区模块数据库上下文
    
    
    创建标识：Senparc - 20261021
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 提供工作区模块数据库上下文

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
