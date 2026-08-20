/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIKernelSenparcEntities.cs
    文件功能描述：AIKernelSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20231220
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.AIKernel.Models
{
    public class AIKernelSenparcEntities : XncfDatabaseDbContext
    {
        public AIKernelSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }

        public DbSet<AIModel> AiModels { get; set; }
        public DbSet<AIVector> AiVectors { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
