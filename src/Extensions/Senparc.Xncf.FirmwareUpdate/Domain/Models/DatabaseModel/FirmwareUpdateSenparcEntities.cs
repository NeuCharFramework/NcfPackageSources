/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FirmwareUpdateSenparcEntities.cs
    文件功能描述：FirmwareUpdateSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
