/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SystemConfigVD.cs
    File description: SystemConfigVD related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Senparc.Areas.Admin.Models.VD
{
    public class BaseSystemConfigVD : BaseAdminVD
    {
    }

    public class SystemConfig_IndexVD : BaseSystemConfigVD
    {
        /// <summary>
        /// Id
        /// </summary>		
        public int Id { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>		
        public string SystemName { get; set; }

        public int IndexTitle { get; set; }

        public List<SelectListItem> ContentItems { get; set; }

        public string NavSysName { get; set; }
    }
}


