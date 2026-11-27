/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopBridgeResource.cs
    File description: localized display metadata for XNCF registration

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.4.10 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.4.12 Localized DesktopBridge pairing and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DesktopBridge;

public sealed class DesktopBridgeResource
{
    /// <summary>
    /// Resolves a DesktopBridge localization key with an optional fallback.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback text.</param>
    /// <returns>The localized or fallback text.</returns>
    public static string Get(string key, string? fallback = null) =>
        ResourceStringLocalizer.Get(typeof(DesktopBridgeResource), key, fallback);
}
