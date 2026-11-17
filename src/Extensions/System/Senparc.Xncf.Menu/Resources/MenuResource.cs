/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MenuResource.cs
    File description: MenuResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Menu
{
    /// <summary>
    /// Localization catalog owned and packaged by the Menu module.
    /// </summary>
    public sealed class MenuResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(MenuResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(MenuResource), key, fallback, arguments);
        }
    }
}
