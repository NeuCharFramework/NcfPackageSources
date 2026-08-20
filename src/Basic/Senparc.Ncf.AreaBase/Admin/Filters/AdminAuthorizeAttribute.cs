/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminAuthorizeAttribute.cs
    文件功能描述：AdminAuthorizeAttribute 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.AreaBase.Admin.Filters
{
    /// <summary>
    /// 当前 Area 授权处理特性
    /// </summary>
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        //AuthorizeAttribute 可以和 MVC 通用：https://docs.microsoft.com/en-us/aspnet/core/razor-pages/filter?view=aspnetcore-2.2
        public static string AuthenticationScheme => SiteConfig.NcfAdminAuthorizeScheme;

        public AdminAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
        public AdminAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
