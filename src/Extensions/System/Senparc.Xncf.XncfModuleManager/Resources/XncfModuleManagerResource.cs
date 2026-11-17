/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleManagerResource.cs
    File description: XncfModuleManagerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.XncfModuleManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the XncfModuleManager module.
    /// </summary>
    public sealed class XncfModuleManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(XncfModuleManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(XncfModuleManagerResource), key, fallback, arguments);
        }
    }
}
