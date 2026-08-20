/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AuthorizeAttributeExtensions.cs
    文件功能描述：AuthorizeAttributeExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;

namespace Senparc.Ncf.Core.Extensions
{

    //TODO： 独立到各个模块中

    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public const string AuthenticationScheme = "NcfUserAuthorizeScheme";
        public UserAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
        public UserAuthorizeAttribute()
        {
            base.AuthenticationSchemes = AuthenticationScheme;
        }
    }
}
