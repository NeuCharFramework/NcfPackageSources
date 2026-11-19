/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerResource.cs
    File description: AgentsManagerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.12.0-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AgentsManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the AgentsManager module.
    /// </summary>
    public sealed class AgentsManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AgentsManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AgentsManagerResource), key, fallback, arguments);
        }
    }
}
