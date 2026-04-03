using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysMenu_Request
    {

    }

    /// <summary>
    ///Menu creation request
    /// </summary>
    public class SysMenu_CreateOrUpdateRequest
    {
        [MaxLength(50)]
        public string Id { get; set; }

        [MaxLength(150)]
        [Required]
        public string MenuName { get; set; }

        /// <summary>
        ///parent menu
        /// </summary>
        [MaxLength(50)]
        public string ParentId { get; set; }

        [MaxLength(350)]
        public string Url { get; set; }

        /// <summary>
        /// icon
        /// </summary>
        [MaxLength(50)]
        public string Icon { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// is visible
        /// </summary>
        public bool Visible { get; set; }

        public string ResourceCode { get; set; }

        public bool IsLocked { get; set; }

        public Ncf.Core.Models.DataBaseModel.MenuType MenuType { get; set; }
    }
}
