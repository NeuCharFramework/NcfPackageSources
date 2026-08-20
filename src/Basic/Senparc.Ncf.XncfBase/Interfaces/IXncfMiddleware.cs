/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IXncfMiddleware.cs
    文件功能描述：IXncfMiddleware 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 中间件接口
    /// </summary>
    public interface IXncfMiddleware
    {
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        IApplicationBuilder UseMiddleware(IApplicationBuilder app);
    }
}
