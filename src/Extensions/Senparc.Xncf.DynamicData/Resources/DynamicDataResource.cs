/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: DynamicDataResource.cs
    File description: DynamicDataResource related implementation
    
    
    Created by: Senparc - 20260717
    
    Modified by: Senparc - 20260717
    Change description: v0.6.0-preview2 integrate shared resource localization and improve feature text for the DynamicData module

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DynamicData
{
    /// <summary>
    /// Localization catalog owned and packaged by the DynamicData module.
    /// </summary>
    public sealed class DynamicDataResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DynamicDataResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DynamicDataResource), key, fallback, arguments);
        }
    }
}
