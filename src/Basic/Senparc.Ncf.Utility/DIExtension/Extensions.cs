/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Extensions.cs
    文件功能描述：Extensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    //参考：https://www.cnblogs.com/yuangang/archive/2016/08/08/5743660.html

    public static class Extensions
    {

        public static IApplicationBuilder UseSenparcMvcDI(this IApplicationBuilder builder)
        {
            DI.ServiceProvider = builder.ApplicationServices;
            return builder;
        }
    }

    /// <summary>
    /// TODO:和SenparcDI合并
    /// </summary>
    public static class DI
    {
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
