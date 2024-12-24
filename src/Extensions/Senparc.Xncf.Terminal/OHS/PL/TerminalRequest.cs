using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.Terminal.OHS.PL
{
    public class Terminal_RunRequest : FunctionAppRequestBase
    {
        [MaxLength(300)]
        [Description("> 命令||命令行，如：dir /?")]
        public string CommandLine { get; set; }
    }
}
