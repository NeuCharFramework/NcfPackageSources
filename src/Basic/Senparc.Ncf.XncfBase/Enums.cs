/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Enums.cs
    文件功能描述：Enums 相关实现
    
    
    创建标识：Senparc - 20231105
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 版本更新类型
    /// </summary>
    public enum UpdateVersionType
    {
        NoUpdate = 0,
        MajorUpdate = 1,
        MinorUpdate = 2,
        PatchUpdate = 3,
    }
}
