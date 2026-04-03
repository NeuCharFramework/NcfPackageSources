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
        ///permissions
        /// </summary>
        public DbSet<SysRolePermission> SysRolePermissions { get; set; }

        /// <summary>
        ///system role
        /// </summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>
        /// system role administrator
        /// </summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //If there is no special need, the OnModelCreating method does not need to be written. Registration is already required in Register.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
