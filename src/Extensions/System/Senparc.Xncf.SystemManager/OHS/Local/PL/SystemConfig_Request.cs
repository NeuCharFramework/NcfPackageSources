/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: SystemConfig_Request.cs
    File description: SystemConfig_Request related implementation
    
    
    Created by: Senparc - 20240827
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260707
    Change description: v0.14.2-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260715
    Change description: v0.14.2-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview3 integrate shared resource localization and improve feature text for the SystemManager module

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions.Parameters;

namespace Senparc.Xncf.SystemManager.OHS.Local.PL
{
    public class SystemConfig_UpdateNeuCharAccountRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(SystemManagerResource), "Parameter.SystemManager.AppKey")]
        public string AppKey{ get; set; }

        [Required]
        [Password]
        [MaxLength(100)]
        [LocalizedDescription(typeof(SystemManagerResource), "Parameter.SystemManager.AppSecret")]
        public string AppSecret { get; set; }
    }

    public class SystemConfig_GetRequestTempLogRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(200)]
        [LocalizedDescription(typeof(SystemManagerResource), "Parameter.SystemManager.RequestTempId")]
        public string RequestTempId { get; set; }
    }
}
