/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminResource.cs
    文件功能描述：AdminResource 相关实现
    
    
    创建标识：Senparc - 20260403
    
    修改标识：Senparc - 20260717
    修改描述：v0.1.0 完善后台管理界面、功能表单与多语言资源本地化

----------------------------------------------------------------*/
using System.Globalization;
using System.Resources;

namespace Senparc.Areas.Admin
{
    /// <summary>
    /// Marker class for Admin module localization resources.
    /// Resource files are stored in Resources/AdminResource.{culture}.resx
    ///
    /// Usage in Razor views: @inject IStringLocalizer&lt;AdminResource&gt; AR
    /// Usage in code:        IStringLocalizer&lt;AdminResource&gt; localizer (via DI)
    ///
    /// Supported cultures: zh-CN (default), en, ja, fr, es, ru
    /// To add a new language: copy AdminResource.en.resx, rename to AdminResource.{culture}.resx,
    /// translate the values, and add the culture code to NcfLocalizationOptions.SupportedCultures.
    /// </summary>
    public class AdminResource
    {
        private static readonly ResourceManager ResourceManager =
            new("Senparc.Areas.Admin.AdminResource", typeof(AdminResource).Assembly);

        /// <summary>
        /// Gets a localized Admin resource using the current request UI culture.
        /// This helper is intended for domain code that cannot receive an
        /// <c>IStringLocalizer&lt;AdminResource&gt;</c> through dependency injection.
        /// </summary>
        public static string Get(string key, string fallback = null)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback ?? key;
        }

        /// <summary>
        /// Gets and formats a localized Admin resource using the current request culture.
        /// </summary>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key, fallback), arguments ?? []);
        }
    }
}
