/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfCoreResource.cs
    File description: Related implementation.

    Created by: Senparc - 20260730

    Modified by: Senparc - 20260731
    Change description: v0.27.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.Core;

/// <summary>
/// Shared localization catalog for core validation messages.
/// </summary>
public sealed class NcfCoreResource
{
    /// <summary>
    /// Resolves a localized core resource value.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The localized or fallback value.</returns>
    public static string Get(string key, string fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(NcfCoreResource), key, fallback);
    }
}
