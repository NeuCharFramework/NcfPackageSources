/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IAutoDI.cs
    文件功能描述：IAutoDI 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.DI
{
    /// <summary>
    /// 所有需要自动扫描进行依赖注入的基础接口
    /// <para>默认 DI 使用 AddScoped 方法，如果需要强制使用其他方法，请在子类上使用 [AutoDIType(typeName)] 特性标签</para>
    /// </summary>
    public interface IAutoDI
    {
    }
}
