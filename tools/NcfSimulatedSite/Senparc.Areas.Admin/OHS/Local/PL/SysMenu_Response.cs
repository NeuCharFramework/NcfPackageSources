using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.PL
{
    public class SysMenu_Response
    {

    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysMenu_CreateOrUpdateResponse
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysMenu_MenuTreeResponse
    {
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuTreeItemDto> Items { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysMenu_MenusResponse
    {
        public IEnumerable<Ncf.Core.Models.DataBaseModel.SysMenuDto> Items { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SysMenu_GetMenuResponse
    {
        public Ncf.Core.Models.DataBaseModel.SysMenuDto Item { get; set; }
    }
}
