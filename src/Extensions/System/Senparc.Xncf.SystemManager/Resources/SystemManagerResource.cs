/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: SystemManagerResource.cs
    File description: SystemManagerResource related implementation
    
    
    Created by: Senparc - 20260717
    
    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview3 integrate shared resource localization and improve feature text for the SystemManager module

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemManager module.
    /// </summary>
    public sealed class SystemManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemManagerResource), key, fallback, arguments);
        }
    }
}
