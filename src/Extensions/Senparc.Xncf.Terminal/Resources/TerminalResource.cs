/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: TerminalResource.cs
    File description: TerminalResource related implementation
    
    
    Created by: Senparc - 20260717
    
    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Terminal
{
    /// <summary>
    /// Localization catalog owned and packaged by the Terminal module.
    /// </summary>
    public sealed class TerminalResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(TerminalResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(TerminalResource), key, fallback, arguments);
        }
    }
}
