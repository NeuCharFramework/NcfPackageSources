/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemPermissionResource.cs
    File description: SystemPermissionResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.15.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemPermission
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemPermission module.
    /// </summary>
    public sealed class SystemPermissionResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemPermissionResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemPermissionResource), key, fallback, arguments);
        }
    }
}
