/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConnectorsResource.cs
    File description: localized display metadata for XNCF registration

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Connectors;

public sealed class ConnectorsResource
{
    /// <summary>
    /// Resolves a connector localization key with an optional fallback.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback text when no localized value exists.</param>
    /// <returns>The localized or fallback text.</returns>
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(ConnectorsResource), key, fallback);

    /// <summary>
    /// Resolves and formats a connector localization key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback format string.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The localized formatted text.</returns>
    public static string Format(string key, string fallback, params object[] arguments) =>
        ResourceStringLocalizer.Format(typeof(ConnectorsResource), key, fallback, arguments);
}
