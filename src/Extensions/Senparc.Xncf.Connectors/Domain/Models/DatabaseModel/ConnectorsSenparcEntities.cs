/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorsSenparcEntities.cs
    文件功能描述：连接器模块数据库上下文
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 提供连接器实例数据上下文

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
