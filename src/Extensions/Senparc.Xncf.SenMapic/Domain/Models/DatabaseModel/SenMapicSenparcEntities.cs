/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicSenparcEntities.cs
    文件功能描述：SenMapicSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20250114
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.SenMapic;
using Senparc.Xncf.SenMapic.Models;

public class SenMapicSenparcEntities : XncfDatabaseDbContext
{
    public SenMapicSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Color> Colors { get; set; }
    public DbSet<SenMapicTask> SenMapicTasks { get; set; }
    public DbSet<SenMapicTaskItem> SenMapicTaskItems { get; set; }
} 