using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// 角色菜单表
    /// </summary>
    [Table("SysRolePermissions")]
    public class SysRolePermission : EntityBase<int>
    {
        /* 注意：这里 Table 如果用 SysPermissions，将和 SQL Server 的系统表冲突
         * 参考：https://docs.microsoft.com/zh-cn/sql/relational-databases/system-compatibility-views/sys-syspermissions-transact-sql?redirectedfrom=MSDN&view=sql-server-ver15
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
        /// 角色代码
        /// </summary>
        [MaxLength(150)]
        public string RoleCode { get; set; }

        /// <summary>
        /// 资源（按钮）代码
        /// </summary>
        [MaxLength(150)]
        public string ResourceCode { get; set; }

        /// <summary>
        /// 角色Id
        /// </summary>
        [MaxLength(150)]
        public string RoleId { get; set; }

        /// <summary>
        /// 是否是菜单
        /// </summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// 权限Id(菜单或者是按钮)
        /// </summary>
        [MaxLength(150)]
        public string PermissionId { get; set; }
    }
}
