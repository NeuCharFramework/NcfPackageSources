/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemPermissionSenparcEntities.cs
    File description: SystemPermissionSenparcEntities related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<SysRolePermission> SysRolePermissions { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
