/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModuleManagerSenparcEntities.cs
    文件功能描述：XncfModuleManagerSenparcEntities 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.XncfModuleManager.Models
{
    public class XncfModuleManagerSenparcEntities : XncfDatabaseDbContext
    {
        public XncfModuleManagerSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {

        }

        /// <summary>
        /// 扩展模块
        /// </summary>
        public DbSet<XncfModule> XncfModules { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
