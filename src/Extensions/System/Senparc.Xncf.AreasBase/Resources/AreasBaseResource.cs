/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreasBaseResource.cs
    File description: AreasBaseResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.13.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.13.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AreasBase
{
    /// <summary>
    /// Localization catalog owned and packaged by the AreasBase module.
    /// </summary>
    public sealed class AreasBaseResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AreasBaseResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AreasBaseResource), key, fallback, arguments);
        }
    }
}
