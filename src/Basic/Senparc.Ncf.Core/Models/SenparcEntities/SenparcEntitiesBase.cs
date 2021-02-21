using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    public abstract class SenparcEntitiesBase : SenparcEntitiesDbContextBase, ISenparcEntitiesDbContext
    {
        #region 系统表（无特殊情况不要修改）

        /// <summary>
        /// 系统设置
        /// </summary>
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public virtual DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 菜单
        /// </summary>
        public DbSet<SysMenu> SysMenus { get; set; }

        /// <summary>
        /// 菜单下面的按钮
        /// </summary>
        public DbSet<SysButton> SysButtons { get; set; }

        /// <summary>
        /// 系统角色
        /// </summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>
        /// 角色菜单表
        /// </summary>
        public DbSet<SysPermission> SysPermission { get; set; }

        /// <summary>
        /// 角色人员表
        /// </summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        /// <summary>
        /// 用户积分日志
        /// </summary>
        public virtual DbSet<PointsLog> PointsLogs { get; set; }

        /// <summary>
        /// 用户支付日志
        /// </summary>

        public virtual DbSet<AccountPayLog> AccountPayLogs { get; set; }

        /// <summary>
        /// 扩展模块
        /// </summary>
        public DbSet<XncfModule> XncfModules { get; set; }


        #endregion


        protected SenparcEntitiesBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Console.WriteLine("\t SenparcEntitiesBase OnModelCreating");

            #region 不可修改系统表
            modelBuilder.ApplyConfiguration(new XncfModuleAccountConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AccountConfigurationMapping());
            modelBuilder.ApplyConfiguration(new AccountPayLogConfigurationMapping());
            modelBuilder.ApplyConfiguration(new PointsLogConfigurationMapping());

            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
