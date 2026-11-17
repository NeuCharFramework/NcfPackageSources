/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SysRoleDto.cs

    File description: SysRoleDto related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysRoleDto : DtoBase
    {
        [MaxLength(50)]
        public string Id { get; set; }

        /// <summary>
        /// role name
        /// </summary>
        [MaxLength(50)]
        public string RoleName { get; set; }

        /// <summary>
        /// role code
        /// </summary>
        [MaxLength(50)]
        public string RoleCode { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
