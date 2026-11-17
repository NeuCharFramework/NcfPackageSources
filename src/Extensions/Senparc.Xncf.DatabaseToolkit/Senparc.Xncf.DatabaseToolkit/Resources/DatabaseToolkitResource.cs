/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: DatabaseToolkitResource.cs
    File description: DatabaseToolkitResource related implementation
    
    
    Created by: Senparc - 20260717
    
    Modified by: Senparc - 20260717
    Change description: v0.26.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DatabaseToolkit
{
    /// <summary>
    /// Localization catalog owned and packaged by the DatabaseToolkit module.
    /// </summary>
    public sealed class DatabaseToolkitResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DatabaseToolkitResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DatabaseToolkitResource), key, fallback, arguments);
        }
    }
}
