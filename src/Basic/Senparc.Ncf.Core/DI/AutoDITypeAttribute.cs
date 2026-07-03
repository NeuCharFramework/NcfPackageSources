/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AutoDITypeAttribute.cs
    文件功能描述：AutoDITypeAttribute 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.DI
{
    /// <summary>
    /// 自动依赖注入特性标签
    /// </summary>
    public class AutoDITypeAttribute : Attribute
    {
        public DILifecycleType DILifecycleType { get; set; } = DILifecycleType.Scoped;

        public AutoDITypeAttribute() { }

        public AutoDITypeAttribute(DILifecycleType diLifecycleType)
        {
            DILifecycleType = diLifecycleType;
        }
    }
}
