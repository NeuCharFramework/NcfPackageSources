/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Enum.cs
    文件功能描述：Enum 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager
{
    public enum WeixinPlatformType
    {
        WeChat_OfficialAccount = 1,
        WeChat_MiniProgram = 2,
        WeChat_Work = 4,
        WeChat_Open = 8,
        WeChat_Tenpay = 16,
    }
}
