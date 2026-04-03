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
        ///role code
        /// </summary>
        [MaxLength(50)]
        public string RoleCode { get; set; }

        /// <summary>
        ///enable
        /// </summary>
        public bool Enabled { get; set; }
    }
}
