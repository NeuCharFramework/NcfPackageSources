/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TerminalRequest.cs
    文件功能描述：TerminalRequest 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.22.0-preview2 为 Terminal 模块接入统一资源本地化并优化功能文案

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
        [LocalizedDescription(typeof(TerminalResource), "Parameter.Terminal.Command")]
        public string CommandLine { get; set; }
    }
}
