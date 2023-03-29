using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.SystemPermission.Models
{
    public class SystemPermissionSenparcEntities : XncfDatabaseDbContext
    {
        public SystemPermissionSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>
        /// 权限
        /// </summary>
        public DbSet<SysRolePermission> SysRolePermissions { get; set; }

        /// <summary>
        /// 系统角色
        /// </summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>
        /// 系统角色管理员
        /// </summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //如无特殊需需要，OnModelCreating 方法可以不用写，已经在 Register 中要求注册
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
