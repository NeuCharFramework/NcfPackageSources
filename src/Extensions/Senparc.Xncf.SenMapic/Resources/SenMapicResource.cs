/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicResource.cs
    File description: SenMapicResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>
    /// Localization catalog owned and packaged by the SenMapic module.
    /// </summary>
    public sealed class SenMapicResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SenMapicResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SenMapicResource), key, fallback, arguments);
        }
    }
}
