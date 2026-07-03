/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TerminalRequest.cs
    文件功能描述：TerminalRequest 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
