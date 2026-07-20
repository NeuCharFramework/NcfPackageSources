/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileManagerResource.cs
    文件功能描述：FileManagerResource 相关实现
    
    
    创建标识：Senparc - 20260717
    
    修改标识：Senparc - 20260717
    修改描述：v0.3.0-preview2 为 FileManager 模块接入统一资源本地化并优化功能文案

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.FileManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the FileManager module.
    /// </summary>
    public sealed class FileManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(FileManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(FileManagerResource), key, fallback, arguments);
        }
    }
}
