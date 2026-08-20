/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItemVersion.cs
    文件功能描述：PromptItemVersion 相关实现
    
    
    创建标识：Senparc - 20240713
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models
{
    public record class PromptItemVersion
    {
        public string RangeName { get; set; }
        public string Tactic { get; set; }
        public int Aim { get; set; }

        public PromptItemVersion(string rangeName, string tactic, int aim)
        {
            RangeName = rangeName;
            Tactic = tactic;
            Aim = aim;
        }
    }
}
