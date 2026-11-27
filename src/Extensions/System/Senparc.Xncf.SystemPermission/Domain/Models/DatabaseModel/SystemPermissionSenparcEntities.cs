/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemPermissionSenparcEntities.cs
    File description: SystemPermissionSenparcEntities related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.15.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Xncf.SystemPermission.Models
{
    /// <summary>EF Core context for role, role-assignment, and permission entities.</summary>
    public class SystemPermissionSenparcEntities : XncfDatabaseDbContext
    {
        public SystemPermissionSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
        {
        }


        /// <summary>Gets or sets role-permission associations.</summary>
        public DbSet<SysRolePermission> SysRolePermissions { get; set; }

        /// <summary>Gets or sets system roles.</summary>
        public DbSet<SysRole> SysRoles { get; set; }

        /// <summary>Gets or sets role-to-administrator associations.</summary>
        public DbSet<SysRoleAdminUserInfo> SysRoleAdminUserInfos { get; set; }

        //DOT REMOVE OR MODIFY THIS LINE - Entities Point
        //ex. public DbSet<Color> Colors { get; set; }

        //Legacy comment translated to English.
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
