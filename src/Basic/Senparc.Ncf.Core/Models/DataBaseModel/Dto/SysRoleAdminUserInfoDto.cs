using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysRoleAdminUserInfoDto : DtoBase
    {
        /// <summary>
        /// role name
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// role number
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        ///user number
        /// </summary>
        public int AdminAccountId { get; set; }

        /// <summary>
        /// Whether the current user has this role
        /// </summary>
        public bool HasRole { get; set; }
    }
}
