using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    public class SysPermissionDto : DtoBase
    {
        public string RoleId { get; set; }

        public string PermissionId { get; set; }

        public bool IsMenu { get; set; }

        /// <summary>
        ///role code
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// Resource (button) code
        /// </summary>
        public string ResourceCode { get; set; }
    }
}
