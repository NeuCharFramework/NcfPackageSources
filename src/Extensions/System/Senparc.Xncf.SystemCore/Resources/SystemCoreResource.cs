/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemCoreResource.cs
    File description: SystemCoreResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.14.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemCore
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemCore module.
    /// </summary>
    public sealed class SystemCoreResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemCoreResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemCoreResource), key, fallback, arguments);
        }
    }
}
