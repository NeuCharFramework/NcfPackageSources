/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: XncfBuilderResource.cs
    File description: XncfBuilderResource related implementation
    
    
    Created by: Senparc - 20260717
    
    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.XncfBuilder
{
    /// <summary>
    /// Localization catalog owned and packaged by the XncfBuilder module.
    /// </summary>
    public sealed class XncfBuilderResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(XncfBuilderResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(XncfBuilderResource), key, fallback, arguments);
        }
    }
}
