/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: SystemConfigDto.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20241028
    
    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Dto
{
    public class SystemConfigDto
    {
        public int Id { get; set; }
        public string SystemName { get; set; }

        public string MchId { get; set; }

        public string MchKey { get; set; }

        public string TenPayAppId { get; set; }

        public bool? HideModuleManager { get; set; }

        public string FooterContent { get; set; }
    }

    public class SystemConfig_CreateOrUpdateDto
    {
        public int Id { get; set; }
        public string SystemName { get; set; }

        public string MchId { get; set; }

        public string MchKey { get; set; }

        public string TenPayAppId { get; set; }

        public bool? HideModuleManager { get; set; }

        public string FooterContent { get; set; }
    }
}
