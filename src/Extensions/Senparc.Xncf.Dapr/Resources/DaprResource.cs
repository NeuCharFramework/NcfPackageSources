/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DaprResource.cs
    File description: DaprResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.11.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Dapr
{
    /// <summary>
    /// Localization catalog owned and packaged by the Dapr module.
    /// </summary>
    public sealed class DaprResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DaprResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DaprResource), key, fallback, arguments);
        }
    }
}
