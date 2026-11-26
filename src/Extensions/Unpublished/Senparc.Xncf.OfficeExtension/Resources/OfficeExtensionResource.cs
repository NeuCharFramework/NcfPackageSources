/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OfficeExtensionResource.cs
    File description: OfficeExtensionResource related implementation

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate OfficeExtension Register display metadata with shared resource localization

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.OfficeExtension;

public sealed class OfficeExtensionResource
{
    /// <summary>
    /// Resolves an Office extension localization key and returns the fallback when unavailable.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="fallback">The fallback text.</param>
    /// <returns>The localized text or fallback text.</returns>
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(OfficeExtensionResource), key, fallback);
    }

    /// <summary>
    /// Resolves and formats an Office extension localization template.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="fallback">The fallback composite format string.</param>
    /// <param name="arguments">The values used by the format template.</param>
    /// <returns>The localized formatted text.</returns>
    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(OfficeExtensionResource), key, fallback, arguments);
    }
}
