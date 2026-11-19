/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApplicationResource.cs
    File description: ApplicationResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.1.0 integrate shared resource localization and improve feature text for the Application module

    Modified by: Senparc - 20261119
    Change description: v0.1.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Application
{
    /// <summary>
    /// Localization catalog owned and packaged by the Application module.
    /// </summary>
    public sealed class ApplicationResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ApplicationResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ApplicationResource), key, fallback, arguments);
        }
    }
}
