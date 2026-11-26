/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfRegisterText.cs
    File description: localized display metadata for XNCF registration

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.28.2 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.XncfBase;

/// <summary>
/// Localized display metadata for an XNCF register.
/// The resource key and Chinese source text are declared once; the value is
/// resolved for the current UI culture whenever the register is read.
/// </summary>
public sealed class XncfRegisterText
{
    private readonly Type _resourceType;
    private readonly string _keyPrefix;
    private readonly string _menuNameFallback;
    private readonly string _descriptionFallback;

    /// <summary>
    /// Initializes localized register metadata and its fallback display values.
    /// </summary>
    /// <param name="resourceType">The resource type used for localized lookup.</param>
    /// <param name="keyPrefix">The resource-key prefix.</param>
    /// <param name="menuNameFallback">The fallback menu name.</param>
    /// <param name="descriptionFallback">The fallback description.</param>
    private XncfRegisterText(
        Type resourceType,
        string keyPrefix,
        string menuNameFallback,
        string descriptionFallback)
    {
        _resourceType = resourceType;
        _keyPrefix = keyPrefix;
        _menuNameFallback = menuNameFallback;
        _descriptionFallback = descriptionFallback;
    }

    public string MenuName => ResourceStringLocalizer.Get(
        _resourceType,
        $"{_keyPrefix}.MenuName",
        _menuNameFallback);

    public string Description => ResourceStringLocalizer.Get(
        _resourceType,
        $"{_keyPrefix}.Description",
        _descriptionFallback);

    /// <summary>
    /// Creates localized register metadata for a resource type.
    /// </summary>
    /// <typeparam name="TResource">The resource type containing localized strings.</typeparam>
    /// <param name="keyPrefix">The resource-key prefix.</param>
    /// <param name="menuNameFallback">The fallback menu name.</param>
    /// <param name="descriptionFallback">The fallback description.</param>
    /// <returns>The localized register metadata object.</returns>
    /// <exception cref="ArgumentException">Thrown when any key or fallback is blank.</exception>
    public static XncfRegisterText Create<TResource>(
        string keyPrefix,
        string menuNameFallback,
        string descriptionFallback)
        where TResource : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuNameFallback);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptionFallback);

        return new XncfRegisterText(
            typeof(TResource),
            keyPrefix,
            menuNameFallback,
            descriptionFallback);
    }
}
