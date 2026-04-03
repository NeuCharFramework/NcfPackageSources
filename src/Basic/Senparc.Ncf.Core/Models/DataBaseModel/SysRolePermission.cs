using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    ///Character menu table
    /// </summary>
    [Table("SysRolePermissions")]
    public class SysRolePermission : EntityBase<int>
    {
        /* Note: If SysPermissions is used in Table here, it will conflict with the system tables of SQL Server.
         * Reference: https://docs.microsoft.com/zh-cn/sql/relational-databases/system-compatibility-views/sys-syspermissions-transact-sql?redirectedfrom=MSDN&view=sql-server-ver15
         */

        public SysRolePermission()
        {
            AddTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }

        public SysRolePermission(SysPermissionDto item) : this()
        {
            RoleId = item.RoleId;
            IsMenu = item.IsMenu;
            PermissionId = item.PermissionId;
            RoleCode = item.RoleCode;
            ResourceCode = item.ResourceCode;
        }

        /// <summary>
        ///role code
        /// </summary>
        [MaxLength(150)]
        public string RoleCode { get; set; }

        /// <summary>
        /// Resource (button) code
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        /// <summary>
        /// roleId
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }

        /// <summary>
        /// Whether it is a menu
        /// </summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// Permission ID (menu or button)
        /// </summary>
        [MaxLength(150)]
        public string PermissionId { get; set; }
    }
}
