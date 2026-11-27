/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SysRoleDto.cs
    File description: SysRoleDto related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Dto
{
    /// <summary>Base role DTO marker retained for mapping compatibility.</summary>
    public class SysRoleDto
    {
    }

    public class PermissionRequestDto
    {
        /// <summary>
        /// Gets or sets the role identifier receiving the permission.
        /// </summary>
        [Required]
        [MaxLength]
        public string RoleId { get; set; }

        /// <summary>
        /// Gets or sets the menu or button permission identifier.
        /// </summary>
        [Required]
        [MaxLength]
        public string PermissionId { get; set; }

        /// <summary>Gets or sets whether the permission represents a menu.</summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// Gets or sets the stable role code.
        /// </summary>
        //[Required]
        [MaxLength]
        public string RoleCode { get; set; }

        /// <summary>
        /// Gets or sets the optional resource code for a button permission.
        /// </summary>
        //[Required]
        [MaxLength]
        public string ResourceCode { get; set; }
    }

    /// <summary>Contains full role details for edit and detail views.</summary>
    public class SysRoleDetailDto
    {
        /// <summary>
        /// Gets or sets whether the role is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the role identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime AddTime { get; set; }

        /// <summary>
        /// Gets or sets the last-update timestamp.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>Gets or sets the administrator-facing remark.</summary>
        public string AdminRemark { get; set; }
        /// <summary>Gets or sets the internal remark.</summary>
        public string Remark { get; set; }
    }

    /// <summary>Contains the role fields displayed in a paged list.</summary>
    public class SysRoleListDto
    {
        /// <summary>
        /// Gets or sets whether the role is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the role identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime AddTime { get; set; }
        
        /// <summary>
        /// Gets or sets the last-update timestamp.
        /// </summary>
        public DateTime LastUpdateTime { get; set; }
    }

}
