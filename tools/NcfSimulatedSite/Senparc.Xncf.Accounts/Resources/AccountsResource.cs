/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AccountsResource.cs
    文件功能描述：AccountsResource 相关实现
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260717
    修改描述：v0.3.0 为账户模块接入多语言资源与功能文案本地化

----------------------------------------------------------------*/
using System.Globalization;
using System.Resources;

namespace Senparc.Xncf.Accounts
{
    /// <summary>
    /// Marker class for Accounts module localization resources.
    /// </summary>
    public class AccountsResource
    {
        private static readonly ResourceManager ResourceManager =
            new("Senparc.Xncf.Accounts.AccountsResource", typeof(AccountsResource).Assembly);

        public static string Get(string key, string fallback = null)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback ?? key;
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key, fallback), arguments ?? []);
        }
    }
}
