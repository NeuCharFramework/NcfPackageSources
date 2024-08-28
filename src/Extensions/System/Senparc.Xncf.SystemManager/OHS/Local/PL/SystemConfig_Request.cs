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
