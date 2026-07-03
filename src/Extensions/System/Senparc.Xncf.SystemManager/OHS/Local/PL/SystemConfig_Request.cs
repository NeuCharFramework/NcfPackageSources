/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemConfig_Request.cs
    文件功能描述：SystemConfig_Request 相关实现
    
    
    创建标识：Senparc - 20240827
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        [Description("NeuChar AppKey||可在 https://www.neuchar.com/Developer/Developer 页面看到 AppKey")]
        public string AppKey{ get; set; }

        [Required]
        [Password]
        [MaxLength(100)]
        [Description("NeuChar AppSecret||可在 https://www.neuchar.com/Developer/Developer 页面看到 Secret，请勿泄露 Secret！")]
        public string AppSecret { get; set; }
    }
}
