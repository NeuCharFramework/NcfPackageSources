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
    }

    public class SystemConfig_CreateOrUpdateDto
    {
        public int Id { get; set; }
        public string SystemName { get; set; }

        public string MchId { get; set; }

        public string MchKey { get; set; }

        public string TenPayAppId { get; set; }

        public bool? HideModuleManager { get; set; }
    }
}
