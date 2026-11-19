/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChangeNamespaceResource.cs
    File description: ChangeNamespaceResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 integrate shared resource localization and improve feature text for the ChangeNamespace module

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.ChangeNamespace
{
    /// <summary>
    /// Localization catalog owned and packaged by the ChangeNamespace module.
    /// </summary>
    public sealed class ChangeNamespaceResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ChangeNamespaceResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ChangeNamespaceResource), key, fallback, arguments);
        }
    }
}
